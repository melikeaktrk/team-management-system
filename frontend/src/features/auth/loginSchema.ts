import { z } from 'zod';

export const loginSchema = z.object({
  email: z.string().trim().min(1, 'E-posta zorunlu').email('Geçerli bir e-posta girin.'),
  password: z.string().min(1, 'Şifre zorunlu').min(6, 'Şifre en az 6 karakter olmalıdır.'),
});

export type LoginFormValues = z.infer<typeof loginSchema>;
