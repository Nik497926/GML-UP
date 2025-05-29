import type { Metadata } from 'next';

import { ModulesPage } from '@/views/modules';

export const metadata: Metadata = {
  title: 'Модули',
};

const Page = () => {
  return <ModulesPage />;
};

export default Page; 