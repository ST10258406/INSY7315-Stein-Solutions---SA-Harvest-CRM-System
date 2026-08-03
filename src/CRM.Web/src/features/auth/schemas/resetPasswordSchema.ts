// src/features/auth/schemas/resetPasswordSchema.ts
import { z } from 'zod';

// Mirrors CRM.Application/Modules/Auth/Commands/ResetPassword/ResetPasswordCommandValidator.cs
// exactly. If the backend rules change, update both together.
export const resetPasswordSchema = z
  .object({
    newPassword: z
      .string()
      .min(8, 'Password must be at least 8 characters')
      .regex(/[0-9]/, 'Password must contain at least one digit')
      .regex(/[A-Z]/, 'Password must contain at least one uppercase letter')
      .regex(/[^a-zA-Z0-9]/, 'Password must contain at least one special character'),
    confirmPassword: z.string().min(1, 'Please confirm your password'),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: 'Passwords do not match',
    path: ['confirmPassword'],
  });

export type ResetPasswordFormValues = z.infer<typeof resetPasswordSchema>;
