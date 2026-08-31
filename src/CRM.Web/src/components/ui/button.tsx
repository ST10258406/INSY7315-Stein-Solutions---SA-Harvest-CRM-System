import { Button as ButtonPrimitive } from "@base-ui/react/button"
import { cva, type VariantProps } from "class-variance-authority"

import { cn } from "@/lib/utils"

const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-full text-sm font-semibold transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50 cursor-pointer active:scale-[0.98]",
  {
    variants: {
      variant: {
        default:
          "bg-[var(--brand)] text-[var(--primary-foreground)] font-bold hover:opacity-90 shadow-[0_2px_8px_rgba(250,223,1,0.45)] dark:shadow-none",
        secondary:
          "bg-[var(--card)] text-[var(--ink)] border border-[var(--border)] shadow-[0_1px_2px_var(--shadow)] hover:bg-[var(--hover)] hover:border-[var(--ink)]",
        dark:
          "bg-[var(--ink)] text-[var(--card)] hover:opacity-90",
        approve:
          "bg-[var(--brand-green)] text-white font-bold hover:opacity-90 shadow-[0_2px_8px_rgba(30,110,60,0.28)]",
        reject:
          "border-[1.5px] border-[var(--brand-red)] bg-transparent text-[var(--brand-red)] font-bold hover:bg-[var(--brand-red)]/10",
        ghost:
          "bg-transparent text-[var(--ink)] hover:bg-[var(--hover)]",
        outline:
          "border border-[var(--border)] bg-transparent text-[var(--ink)] hover:border-[var(--ink)]",
        link: "text-[var(--brand)] underline-offset-4 hover:underline",
      },
      size: {
        default: "h-11 px-5 py-2 text-[13.5px]",
        sm: "h-9 px-3.5 text-[12.5px] rounded-full",
        lg: "h-12 px-6 text-sm rounded-full",
        icon: "size-9 p-0 rounded-full",
        "icon-sm": "size-7 rounded-full",
        "icon-lg": "size-10 rounded-full",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
)

function Button({
  className,
  variant = "default",
  size = "default",
  ...props
}: ButtonPrimitive.Props & VariantProps<typeof buttonVariants>) {
  return (
    <ButtonPrimitive
      data-slot="button"
      className={cn(buttonVariants({ variant, size, className }))}
      {...props}
    />
  )
}

// eslint-disable-next-line react-refresh/only-export-components
export { Button, buttonVariants }
