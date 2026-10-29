import { render, screen, cleanup } from '@testing-library/react';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, afterEach } from 'vitest';
import { AgentMarkdown } from './AgentMarkdown';

const renderMd = (content: string) => render(<AgentMarkdown content={content} />).container;

describe('AgentMarkdown', () => {
  afterEach(cleanup);

  it('renders a typical agent reply: bold heading, bullets with bold values, closing paragraph', () => {
    const container = renderMd(
      [
        '**Dashboard Summary (19 Oct 2026)**  ',
        '',
        '- Total donors: **6**  ',
        '- Active donors: **3**  ',
        '- Your open tasks: **3** (none overdue)  ',
        '',
        'Let me know if you’d like details on any of these areas.',
      ].join('\n'),
    );

    expect(screen.getByText('Dashboard Summary (19 Oct 2026)').tagName).toBe('STRONG');
    const items = container.querySelectorAll('ul > li');
    expect(items).toHaveLength(3);
    expect(items[0]).toHaveTextContent('Total donors: 6');
    expect(items[0].querySelector('strong')).toHaveTextContent('6');
    expect(container.textContent).not.toContain('**');
    expect(screen.getByText(/Let me know/).tagName).toBe('SPAN');
  });

  it('renders numbered lists, headings, italic and inline code', () => {
    const container = renderMd('### Next steps\n1. Call *FoodCorp*\n2. Check `DON-2026-00012`');

    expect(screen.getByText('Next steps').tagName).toBe('P');
    expect(container.querySelectorAll('ol > li')).toHaveLength(2);
    expect(screen.getByText('FoodCorp').tagName).toBe('EM');
    expect(screen.getByText('DON-2026-00012').tagName).toBe('CODE');
  });

  it('renders a table', () => {
    const container = renderMd('| Company | Status |\n|---|---|\n| FoodCorp SA | Active |\n| Fresh Farms | Lapsed |');

    expect(container.querySelectorAll('th')).toHaveLength(2);
    expect(container.querySelectorAll('tbody tr')).toHaveLength(2);
    expect(screen.getByText('Fresh Farms').tagName).toBe('TD');
  });

  it('never creates HTML elements from model text', () => {
    const container = renderMd('<img src=x onerror="alert(1)"> <script>alert(2)</script> **<b>hi</b>**');

    expect(container.querySelector('img, script, b')).toBeNull();
    expect(container.textContent).toContain('<img src=x onerror="alert(1)">');
  });

  it('renders links and images as text only, never as anchors', () => {
    const container = renderMd('See [the portal](https://evil.example/login) and ![x](https://evil.example/x.png)');

    expect(container.querySelector('a, img')).toBeNull();
    expect(container.textContent).toContain('the portal');
    expect(container.textContent).not.toContain('https://evil.example/login');
  });

  it('treats a lone table-like row as a paragraph instead of hanging', () => {
    const container = renderMd('| not a table |\nnext line');

    expect(container.textContent).toContain('| not a table |');
    expect(container.textContent).toContain('next line');
  });

  it('renders gpt-oss style tables: no outer pipes, single and Unicode dashes', () => {
    const container = renderMd('Region | Donors\n‑‑‑ | –\nGP | 12\nKZN | 4');

    expect(container.querySelectorAll('th')).toHaveLength(2);
    expect(container.querySelectorAll('tbody tr')).toHaveLength(2);
    expect(container.textContent).not.toContain('|');
  });

  it('starts a table straight after a paragraph line', () => {
    renderMd('Here is the breakdown:\n| RM | Lapsed |\n|-|-|\n| Thandi | 7 |');

    expect(screen.getByText('Here is the breakdown:').tagName).toBe('SPAN');
    expect(screen.getByText('Thandi').tagName).toBe('TD');
  });

  it('applies divider alignment and right-aligns numeric columns', () => {
    renderMd('| Name | Status | Count |\n|:---|:---:|---|\n| A | Active | 1,204 |\n| B | Lapsed | R 3 500 |');

    expect(screen.getByText('A')).toHaveStyle({ textAlign: 'left' });
    expect(screen.getByText('Active')).toHaveStyle({ textAlign: 'center' });
    expect(screen.getByText('1,204')).toHaveStyle({ textAlign: 'right' });
  });

  it('handles <br> and escaped pipes in cells, and pads ragged rows', () => {
    const container = renderMd('| A | B | C |\n|---|---|---|\n| one<br>two | x \\| y |\n| <script>alert(1)</script> | 2 | 3 | 4 |');

    const rows = container.querySelectorAll('tbody tr');
    expect(rows[0].querySelectorAll('td')).toHaveLength(3);
    expect(rows[0].querySelector('td br')).not.toBeNull();
    expect(rows[0].querySelectorAll('td')[1]).toHaveTextContent('x | y');
    expect(rows[1].querySelectorAll('td')).toHaveLength(3);
    expect(container.querySelector('script')).toBeNull();
    expect(container.textContent).toContain('<script>alert(1)</script>');
  });

  it('does not treat bullets or a lone rule as a table', () => {
    const container = renderMd('Total\n---\n- a | b\n- c');

    expect(container.querySelector('table')).toBeNull();
  });
});
