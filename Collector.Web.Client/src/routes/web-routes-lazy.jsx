import {lazy} from 'react';

const routes = [
    //web pages
    { path: '/web',                   Element:lazy(() => import('@/app/web/page')) },
    { path: '/web/domains',           Element:lazy(() => import('@/app/web/domains/page')) },
    { path: '/web/domains/edit/:id',  Element:lazy(() => import('@/app/web/domains/page')) },
    { path: '/web/downloads',         Element:lazy(() => import('@/app/web/downloads/page')) },
    { path: '/web/downloads/details/:id', Element:lazy(() => import('@/app/web/downloads/page')) },
    { path: '/web/articles',          Element:lazy(() => import('@/app/web/articles/page')) },
    { path: '/web/articles/edit/:id', Element:lazy(() => import('@/app/web/articles/page')) },
    { path: '/web/feeds',              Element:lazy(() => import('@/app/feeds/page')) },
    { path: '/web/subjects',          Element:lazy(() => import('@/app/web/subjects/page')) },
    { path: '/web/subjects/edit/:id', Element:lazy(() => import('@/app/web/subjects/page')) },
    { path: '/web/blacklists',        Element:lazy(() => import('@/app/web/blacklists/page')) },
    { path: '/web/blacklists/edit/:id', Element:lazy(() => import('@/app/web/blacklists/page')) },
    { path: '/web/whitelists',        Element:lazy(() => import('@/app/web/whitelists/page')) },
    { path: '/web/whitelists/edit/:id', Element:lazy(() => import('@/app/web/whitelists/page')) }
];

export default routes;
