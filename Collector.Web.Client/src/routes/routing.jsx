import React, { useEffect, useState } from 'react';
import { Routes, Route, useLocation } from 'react-router-dom';
import appRoutes from './app-routes-lazy';
import adminRoutes from './admin-routes-lazy';
import dashRoutes from './dash-routes-lazy';

import RootLayout from '@/layout';
import AdminLayout from '@/app/admin/layout';
import DashLayout from '@/app/dashboard/layout';

const RouteElement = ({ path, Element, layout }) => {
  if (!Element) return null;
  const Layout = layout || RootLayout;
  return <Route key={path} path={path} element={<Layout><Element></Element></Layout>}></Route>
}

export default function BaseRoutes() {
  const location = useLocation();
  const getRootPath = () => window.location.pathname.split('/').filter(a => a != '')[0];
  const [route, setRoute] = useState(getRootPath())

  useEffect(() => {
    const r = getRootPath();
    if (r != route) setRoute(r);
  }, [location]);


  return (<Routes>
    {appRoutes.map(route => RouteElement({...route, layout: RootLayout}))}
    {adminRoutes.map(route => RouteElement({...route, layout: AdminLayout}))}
    {dashRoutes.map(route => RouteElement({...route, layout: DashLayout}))}
  </Routes>);
};
