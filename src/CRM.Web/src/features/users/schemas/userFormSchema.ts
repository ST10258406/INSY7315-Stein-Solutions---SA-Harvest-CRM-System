import { z } from 'zod';

export const newUserFormSchema = z.object({
  firstName: z.string().trim().min(1, 'First name is required'),
  lastName: z.string().trim().min(1, 'Last name is required'),
  email: z.string().trim().min(1, 'Email is required').email('Enter a valid email address'),
  roleId: z.string().trim().min(1, 'Select a role'),
});
export type NewUserFormValues = z.infer<typeof newUserFormSchema>;

export const defaultNewUserValues: NewUserFormValues = {
  firstName: '',
  lastName: '',
  email: '',
  roleId: '',
};

export const editUserFormSchema = z.object({
  firstName: z.string().trim().min(1, 'First name is required'),
  lastName: z.string().trim().min(1, 'Last name is required'),
  email: z.string().trim().min(1, 'Email is required').email('Enter a valid email address'),
});
export type EditUserFormValues = z.infer<typeof editUserFormSchema>;

export const changeRoleFormSchema = z.object({
  roleId: z.string().trim().min(1, 'Select a role'),
});
export type ChangeRoleFormValues = z.infer<typeof changeRoleFormSchema>;
