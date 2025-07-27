import {lazy} from 'react';

const routes = [
    //admin pages
    { path: '/admin',                Element:lazy(() => import('@/app/admin/page')) },
    { path: '/admin/users',          Element:lazy(() => import('@/app/admin/users/page')) },
    { path: '/admin/users/edit/:id', Element:lazy(() => import('@/app/admin/users/edit/page')) }
];

export default routes;