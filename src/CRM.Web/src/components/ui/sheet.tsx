import { Dialog as DialogPrimitive } from "@base-ui/react/dialog"
import { X } from "lucide-react"

import { cn } from "@/lib/utils"

// Right-side slide-out panel, built on the same Base UI Dialog primitive as
// dialog.tsx (full accessibility — focus trap, Escape-to-close, backdrop
// click — for free), just repositioned/animated to slide in from the right
// instead of appearing centered. Matches the Claude design's Email Panel.

const Sheet = DialogPrimitive.Root
const SheetTrigger = DialogPrimitive.Trigger
const SheetClose = DialogPrimitive.Close

function SheetBackdrop({ className, ...props }: DialogPrimitive.Backdrop.Props) {
  return (
    <DialogPrimitive.Backdrop
      data-slot="sheet-backdrop"
      className={cn(
        "fixed inset-0 z-50 bg-[rgba(20,20,15,0.42)] transition-opacity duration-200 data-[starting-style]:opacity-0 data-[ending-style]:opacity-0",
        className
      )}
      {...props}
    />
  )
}

interface SheetContentProps extends DialogPrimitive.Popup.Props {
  /** Hide the built-in top-right close button (defaults to shown). */
  hideCloseButton?: boolean
}

function SheetContent({ className, children, hideCloseButton, ...props }: SheetContentProps) {
  return (
    <DialogPrimitive.Portal>
      <SheetBackdrop />
      <DialogPrimitive.Popup
        data-slot="sheet-content"
        className={cn(
          "fixed inset-y-0 right-0 z-50 flex w-[620px] max-w-[92vw] flex-col border-l border-[var(--border)] bg-[var(--card)] shadow-[-24px_0_60px_rgba(20,20,15,0.28)] outline-none transition-transform duration-300 ease-[cubic-bezier(0.32,0.72,0.24,1)] data-[starting-style]:translate-x-full data-[ending-style]:translate-x-full",
          className
        )}
        {...props}
      >
        {children}
        {!hideCloseButton && (
          <DialogPrimitive.Close
            aria-label="Close"
            className="absolute right-5.5 top-5.5 flex h-9 w-9 items-center justify-center rounded-full border border-[var(--border)] bg-[var(--card)] text-[var(--icon)] transition-colors hover:bg-[var(--hover)]"
          >
            <X className="h-3.75 w-3.75 stroke-[2]" />
          </DialogPrimitive.Close>
        )}
      </DialogPrimitive.Popup>
    </DialogPrimitive.Portal>
  )
}

function SheetHeader({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="sheet-header"
      className={cn("flex-shrink-0 border-b border-[var(--divider)] px-6.5 pt-5.5 pb-4 pr-14", className)}
      {...props}
    />
  )
}

function SheetTitle({ className, ...props }: DialogPrimitive.Title.Props) {
  return (
    <DialogPrimitive.Title
      data-slot="sheet-title"
      className={cn("m-0 text-[19px] font-extrabold tracking-tight text-[var(--ink)]", className)}
      {...props}
    />
  )
}

function SheetDescription({ className, ...props }: DialogPrimitive.Description.Props) {
  return (
    <DialogPrimitive.Description
      data-slot="sheet-description"
      className={cn("m-0 mt-1.25 text-[12.5px] font-medium text-[var(--muted-c)]", className)}
      {...props}
    />
  )
}

export {
  Sheet,
  SheetTrigger,
  SheetClose,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
}
