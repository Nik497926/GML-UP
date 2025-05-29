import { Metadata } from 'next';

import { FilesPage } from '@/views/files';

export const metadata: Metadata = {
  title: 'Файловый менеджер',
};

export default function Page() {
  return <FilesPage />;
} 