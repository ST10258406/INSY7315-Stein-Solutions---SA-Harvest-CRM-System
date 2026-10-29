import { createRef } from 'react';
import { act, render, screen, cleanup } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import '@testing-library/jest-dom/vitest';
import { describe, it, expect, afterEach } from 'vitest';
import { Building2, MapPin } from 'lucide-react';
import { AccordionFormLayout, type AccordionFormLayoutHandle } from './AccordionFormLayout';
import { FormAccordionSection } from './FormAccordionSection';

function renderForm(errorCounts: Record<string, number> = {}, defaultOpen: string[] = []) {
  const ref = createRef<AccordionFormLayoutHandle>();
  render(
    <AccordionFormLayout ref={ref} sectionIds={['organisation', 'location']} defaultOpen={defaultOpen}>
      <FormAccordionSection id="organisation" number={1} title="Organisation & Contact" icon={Building2} errorCount={errorCounts.organisation}>
        <label>
          Legal name
          <input />
        </label>
      </FormAccordionSection>
      <FormAccordionSection id="location" number={2} title="Location" icon={MapPin} errorCount={errorCounts.location}>
        <label>
          What3Words
          <input />
        </label>
      </FormAccordionSection>
    </AccordionFormLayout>,
  );
  return ref;
}

const header = (name: RegExp) => screen.getByRole('button', { name });
// Panels stay mounted when closed, so query hidden inputs too and assert visibility.
const field = (name: string) => screen.getByLabelText(name, { selector: 'input' });

describe('FormAccordionSection / AccordionFormLayout', () => {
  afterEach(cleanup);

  it('toggles its content when the header is clicked', async () => {
    const user = userEvent.setup();
    renderForm();
    expect(field('Legal name')).not.toBeVisible();
    expect(header(/organisation & contact/i)).toHaveAttribute('aria-expanded', 'false');

    await user.click(header(/organisation & contact/i));
    expect(field('Legal name')).toBeVisible();
    expect(header(/organisation & contact/i)).toHaveAttribute('aria-expanded', 'true');

    await user.click(header(/organisation & contact/i));
    expect(field('Legal name')).not.toBeVisible();
  });

  it('keeps two sections open at the same time', async () => {
    const user = userEvent.setup();
    renderForm();
    await user.click(header(/organisation & contact/i));
    await user.click(header(/location/i));
    expect(field('Legal name')).toBeVisible();
    expect(field('What3Words')).toBeVisible();
  });

  it('expands and collapses everything', async () => {
    const user = userEvent.setup();
    renderForm();
    await user.click(screen.getByRole('button', { name: 'Expand all' }));
    expect(field('Legal name')).toBeVisible();
    expect(field('What3Words')).toBeVisible();

    await user.click(screen.getByRole('button', { name: 'Collapse all' }));
    expect(field('Legal name')).not.toBeVisible();
    expect(field('What3Words')).not.toBeVisible();
  });

  it('shows the error badge on a collapsed section', () => {
    renderForm({ location: 2, organisation: 1 });
    expect(field('What3Words')).not.toBeVisible();
    expect(header(/location/i)).toHaveTextContent('2 errors');
    expect(header(/organisation & contact/i)).toHaveTextContent('1 error');
  });

  it('keeps typed text when a section is collapsed and reopened', async () => {
    const user = userEvent.setup();
    renderForm({}, ['organisation']);
    await user.type(field('Legal name'), 'Ubuntu Food Co');

    await user.click(header(/organisation & contact/i));
    expect(field('Legal name')).not.toBeVisible();
    await user.click(header(/organisation & contact/i));

    expect(field('Legal name')).toHaveValue('Ubuntu Food Co');
  });

  it('openSections opens the given sections and keeps open ones open', async () => {
    const user = userEvent.setup();
    const ref = renderForm();
    await user.click(header(/organisation & contact/i));

    act(() => ref.current!.openSections(['location']));

    expect(field('Legal name')).toBeVisible();
    expect(field('What3Words')).toBeVisible();
  });

  it('toggles from the keyboard', async () => {
    const user = userEvent.setup();
    renderForm();
    await user.tab(); // Expand all
    await user.tab(); // Collapse all
    await user.tab();
    expect(header(/organisation & contact/i)).toHaveFocus();
    await user.keyboard('{Enter}');
    expect(field('Legal name')).toBeVisible();

    await user.tab(); // into the open panel's input
    await user.tab();
    expect(header(/location/i)).toHaveFocus();
    await user.keyboard(' ');
    expect(field('What3Words')).toBeVisible();
  });
});
