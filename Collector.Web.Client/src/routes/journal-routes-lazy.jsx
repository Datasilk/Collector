import {lazy} from 'react';

const routes = [
    //journal pages
    { path: '/journal', Element:lazy(() => import('@/app/journal/entry')) },
    { path: '/journal/:journalId', Element:lazy(() => import('@/app/journal/entry')) },
    { path: '/journal/:journalId/entry/new', Element:lazy(() => import('@/app/journal/entry')) },
    { path: '/journal/:journalId/entry/:entryId/snapshot/:snapshotId', Element:lazy(() => import('@/app/journal/entry')) },
    { path: '/journal/:journalId/entry/:entryId', Element:lazy(() => import('@/app/journal/entry')) },
];

export default routes;
