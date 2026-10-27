import { createContext, useContext, useState } from 'react';

/** The ids of the open sections, shared by AccordionFormLayout with its sections' contents. */
export const AccordionOpenContext = createContext<string[]>([]);

/** True from the first time section `id` is opened (stays true), e.g. to defer loading until it's looked at. */
export function useSectionOpenedOnce(id: string) {
  const isOpen = useContext(AccordionOpenContext).includes(id);
  const [opened, setOpened] = useState(isOpen);
  if (isOpen && !opened) setOpened(true); // adjust state during render, per React's guidance
  return opened || isOpen;
}
