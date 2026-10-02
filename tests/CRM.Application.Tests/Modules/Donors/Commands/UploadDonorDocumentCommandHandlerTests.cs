using CRM.Application.Tests.Common;
using AutoMapper;
using CRM.Application.Common.Exceptions;
using CRM.Application.Common.Interfaces;
using CRM.Application.Common.Models;
using CRM.Application.Modules.Donors.Commands.UploadDonorDocument;
using CRM.Application.Modules.Donors.Mappings;
using CRM.Application.Modules.Lookups.Mappings;
using CRM.Domain.Entities;
using CRM.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Donors.Commands;

public class UploadDonorDocumentCommandHandlerTests
{
    private readonly IDonorRepository _donorsMock = Substitute.For<IDonorRepository>();
    private readonly IDonorDocumentRepository _documentsMock = Substitute.For<IDonorDocumentRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IBlobStorageService _blobStorageMock = Substitute.For<IBlobStorageService>();
    private readonly ICurrentUserService _currentUserServiceMock = Substitute.For<ICurrentUserService>();
    private readonly IMapper _mapper;
    private readonly UploadDonorDocumentCommandHandler _handler;

    private readonly List<DonorDocument> _added = [];
    private readonly Guid _currentUserId = Guid.NewGuid();

    public UploadDonorDocumentCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<DonorMappingProfile>();
            cfg.AddProfile<LookupsMappingProfile>();
        }, NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _currentUserServiceMock.GetCurrentUserId().Returns(_currentUserId);

        _blobStorageMock
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(ci => Task.FromResult(new BlobUploadResult(ci.ArgAt<string>(1), "https://blob.test/" + ci.ArgAt<string>(1))));

        _documentsMock
            .AddAsync(Arg.Do<DonorDocument>(d => _added.Add(d)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _documentsMock
            .GetActiveByDonorAndTypeAsync(Arg.Any<Guid>(), Arg.Any<DocumentType>(), Arg.Any<CancellationToken>())
            .Returns([]);

        _handler = new UploadDonorDocumentCommandHandler(
            _donorsMock, _documentsMock, _unitOfWorkMock, _blobStorageMock, _currentUserServiceMock, _mapper);
    }

    private Guid SetupExistingDonor()
    {
        var donorId = Guid.NewGuid();
        _donorsMock.ExistsAsync(donorId, Arg.Any<CancellationToken>()).Returns(true);
        return donorId;
    }

    private static UploadDonorDocumentCommand MakeCommand(Guid donorId) => new()
    {
        DonorId = donorId,
        DocumentType = "BBBEECertificate",
        FileStream = TestFiles.PdfStream(),
        OriginalFileName = "cert.pdf",
        ContentType = "application/pdf",
        FileSizeBytes = 1024
    };

    [Fact]
    public async Task Handle_NoExistingActiveDocument_CreatesNewActiveDocument()
    {
        var donorId = SetupExistingDonor();

        var result = await _handler.Handle(MakeCommand(donorId), CancellationToken.None);

        var created = Assert.Single(_added);
        Assert.True(created.IsActive);
        Assert.Equal(DocumentType.BBBEECertificate, created.DocumentType);
        Assert.Equal(_currentUserId, created.UploadedByUserId);
        Assert.True(result.IsActive);
        Assert.Equal("BBBEECertificate", result.DocumentType);
        Assert.Equal("cert.pdf", result.OriginalFileName);
    }

    [Fact]
    public async Task Handle_ExistingActiveDocumentOfSameType_SoftDeletesOldAndSavesOnce()
    {
        var donorId = SetupExistingDonor();

        var existing = new DonorDocument
        {
            Id = Guid.NewGuid(),
            DonorId = donorId,
            DocumentType = DocumentType.BBBEECertificate,
            FileName = "old_cert.pdf",
            BlobStoragePath = "donors/old/path",
            IsActive = true
        };

        _documentsMock
            .GetActiveByDonorAndTypeAsync(donorId, DocumentType.BBBEECertificate, Arg.Any<CancellationToken>())
            .Returns([existing]);

        await _handler.Handle(MakeCommand(donorId), CancellationToken.None);

        Assert.False(existing.IsActive);
        Assert.Single(_added);
        Assert.True(_added[0].IsActive);

        // Supersede + insert must land in one transaction, so exactly one save.
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnlyQueriesActiveDocumentsOfTheSameType()
    {
        // Documents of other types are never fetched, so they can't be superseded.
        var donorId = SetupExistingDonor();

        await _handler.Handle(MakeCommand(donorId), CancellationToken.None); // BBBEECertificate

        await _documentsMock.Received(1).GetActiveByDonorAndTypeAsync(
            donorId, DocumentType.BBBEECertificate, Arg.Any<CancellationToken>());
        await _documentsMock.DidNotReceive().GetActiveByDonorAndTypeAsync(
            Arg.Any<Guid>(), DocumentType.Signature, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistentDonor_ThrowsNotFoundException()
    {
        _donorsMock.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(MakeCommand(Guid.NewGuid()), CancellationToken.None));

        await _blobStorageMock.DidNotReceive().UploadAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UploadsToBlobStorageWithExpectedPathPattern()
    {
        var donorId = SetupExistingDonor();

        await _handler.Handle(MakeCommand(donorId), CancellationToken.None);

        await _blobStorageMock.Received(1).UploadAsync(
            Arg.Any<Stream>(),
            Arg.Is<string>(p => p.StartsWith($"donors/{donorId}/BBBEECertificate/") && p.EndsWith(".pdf") && !p.Contains("cert")),
            "application/pdf");
    }
}
