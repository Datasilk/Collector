import {lazy} from 'react';

const routes = [
    //admin pages
    { path: '/admin',                   Element:lazy(() => import('@/app/admin/page')) },
    { path: '/admin/users',             Element:lazy(() => import('@/app/admin/users/page')) },
    { path: '/admin/users/edit/:id',    Element:lazy(() => import('@/app/admin/users/edit/page')) },
    { path: '/admin/openai',            Element:lazy(() => import('@/app/admin/openai/page')) },
    { path: '/admin/ollama',            Element:lazy(() => import('@/app/admin/ollama/page')) }
];

export default routes;