import {lazy} from 'react';
import { Navigate } from 'react-router-dom';

const routes = [
    //end-user pages
    {path:'/',                          Element:() => <Navigate to="/login" replace />},
    {path:'/signup/:token?',            Element:lazy(() => import('@/app/account/signup/page'))},
    {path:'/login',                     Element:lazy(() => import('@/app/account/login/page'))},
    {path:'/activate/:hash',            Element:lazy(() => import('@/app/account/activate/page'))},
    {path:'/forgot-password/',          Element:lazy(() => import('@/app/account/forgot-password/page'))},
    {path:'/reset-password/:hash',      Element:lazy(() => import('@/app/account/create-password/page'))},
    {path:'/create-password/:hash',     Element:lazy(() => import('@/app/account/create-password/page'))},
    {path:'/account',                   Element:lazy(() => import('@/app/account/page'))}
];

export default routes;