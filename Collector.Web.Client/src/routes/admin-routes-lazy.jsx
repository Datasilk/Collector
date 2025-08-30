import {lazy} from 'react';

const routes = [
    //admin pages
    { path: '/admin',                   Element:lazy(() => import('@/app/admin/page')) },
    { path: '/admin/users',             Element:lazy(() => import('@/app/admin/users/page')) },
    { path: '/admin/users/edit/:id',    Element:lazy(() => import('@/app/admin/users/edit/page')) },
    { path: '/admin/domains',           Element:lazy(() => import('@/app/admin/domains/page')) },
    { path: '/admin/domains/edit/:id',  Element:lazy(() => import('@/app/admin/domains/page')) },
    { path: '/admin/downloads',         Element:lazy(() => import('@/app/admin/downloads/page')) },
    { path: '/admin/downloads/details/:id', Element:lazy(() => import('@/app/admin/downloads/page')) },
    { path: '/admin/articles',          Element:lazy(() => import('@/app/admin/articles/page')) },
    { path: '/admin/articles/edit/:id', Element:lazy(() => import('@/app/admin/articles/page')) },
    { path: '/admin/subjects',          Element:lazy(() => import('@/app/admin/subjects/page')) },
    { path: '/admin/subjects/edit/:id', Element:lazy(() => import('@/app/admin/subjects/page')) },
    { path: '/admin/blacklists',        Element:lazy(() => import('@/app/admin/blacklists/page')) },
    { path: '/admin/blacklists/edit/:id', Element:lazy(() => import('@/app/admin/blacklists/page')) },
    { path: '/admin/whitelists',        Element:lazy(() => import('@/app/admin/whitelists/page')) },
    { path: '/admin/whitelists/edit/:id', Element:lazy(() => import('@/app/admin/whitelists/page')) }
];

export default routes;