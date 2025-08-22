import {lazy} from 'react';

const routes = [
    //journal pages
    { path: '/journal', Element:lazy(() => import('@/app/journal/details/page')) },
    { path: '/journal/:journalId', Element:lazy(() => import('@/app/journal/details/page')) },
    { path: '/journal/:journalId/entry/:entryId', Element:lazy(() => import('@/app/journal/entry/page')) },
];

export default routes;
