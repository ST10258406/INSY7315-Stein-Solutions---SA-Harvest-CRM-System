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
using MockQueryable.NSubstitute;
using NSubstitute;

namespace CRM.Application.Tests.Modules.Donors.Commands;

public class UploadDonorDocumentCommandHandlerTests
{
    private readonly IApplicationDbContext _contextMock;
    private readonly IBlobStorageService _blobStorageMock;
    private readonly ICurrentUserService _currentUserServiceMock;
    private readonly IMapper _mapper;
    private readonly UploadDonorDocumentCommandHandler _handler;

    private readonly List<Donor> _donors = [];
    private readonly List<DonorDocument> _documents = [];
    private readonly Guid _currentUserId = Guid.NewGuid();

    public UploadDonorDocumentCommandHandlerTests()
    {
        _contextMock = Substitute.For<IApplicationDbContext>();
        _blobStorageMock = Substitute.For<IBlobStorageService>();
        _currentUserServiceMock = Substitute.For<ICurrentUserService>();

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

        var donorsDbSet = _donors.BuildMockDbSet();
        _contextMock.Donors.Returns(donorsDbSet);

        var documentsDbSet = _documents.BuildMockDbSet();
        _contextMock.DonorDocuments.Returns(documentsDbSet);
        _contextMock.DonorDocuments.Add(Arg.Do<DonorDocument>(d => _documents.Add(d)));

        _handler = new UploadDonorDocumentCommandHandler(_contextMock, _blobStorageMock, _currentUserServiceMock, _mapper);
    }

    private Donor AddDonor()
    {
        var donor = new Donor { Id = Guid.NewGuid(), CompanyName = "Test Co" };
        _donors.Add(donor);
        return donor;
    }

    private static UploadDonorDocumentCommand MakeCommand(Guid donorId) => new()
    {
        DonorId = donorId,
        DocumentType = "BBBEECertificate",
        FileStream = new MemoryStream([1, 2, 3]),
        OriginalFileName = "cert.pdf",
        ContentType = "application/pdf",
        FileSizeBytes = 1024
    };

    [Fact]
    public async Task Handle_NoExistingActiveDocument_CreatesNewActiveDocument()
    {
        var donor = AddDonor();
        var command = MakeCommand(donor.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        var created = Assert.Single(_documents);
        Assert.True(created.IsActive);
        Assert.Equal(DocumentType.BBBEECertificate, created.DocumentType);
        Assert.True(result.IsActive);
        Assert.Equal("BBBEECertificate", result.DocumentType);
        Assert.Equal("cert.pdf", result.OriginalFileName);
    }

    [Fact]
    public async Task Handle_ExistingActiveDocumentOfSameType_SoftDeletesOldAndKeepsBothRows()
    {
        var donor = AddDonor();
        var existing = new DonorDocument
        {
            Id = Guid.NewGuid(),
            DonorId = donor.Id,
            DocumentType = DocumentType.BBBEECertificate,
            FileName = "old_cert.pdf",
            BlobStoragePath = "donors/old/path",
            IsActive = true
        };
        _documents.Add(existing);

        var command = MakeCommand(donor.Id);
        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(2, _documents.Count);
        Assert.False(existing.IsActive);
        Assert.Single(_documents, d => d.IsActive);
        await _contextMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingActiveDocumentOfDifferentType_IsNotAffected()
    {
        var donor = AddDonor();
        var signature = new DonorDocument
        {
            Id = Guid.NewGuid(),
            DonorId = donor.Id,
            DocumentType = DocumentType.Signature,
            FileName = "signature.png",
            BlobStoragePath = "donors/sig/path",
            IsActive = true
        };
        _documents.Add(signature);

        var command = MakeCommand(donor.Id); // BBBEECertificate
        await _handler.Handle(command, CancellationToken.None);

        Assert.True(signature.IsActive);
        Assert.Equal(2, _documents.Count);
    }

    [Fact]
    public async Task Handle_NonExistentDonor_ThrowsNotFoundException()
    {
        var command = MakeCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UploadsToBlobStorageWithExpectedPathPattern()
    {
        var donor = AddDonor();
        var command = MakeCommand(donor.Id);

        await _handler.Handle(command, CancellationToken.None);

        await _blobStorageMock.Received(1).UploadAsync(
            Arg.Any<Stream>(),
            Arg.Is<string>(p => p.StartsWith($"donors/{donor.Id}/BBBEECertificate/") && p.EndsWith("cert.pdf")),
            "application/pdf");
    }
}
