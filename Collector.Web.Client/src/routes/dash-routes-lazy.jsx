import {lazy} from 'react';

const routes = [
    //dashboard pages
    { path: '/dashboard', Element:lazy(() => import('@/app/dashboard/dashboard')) },
];

export default routes;