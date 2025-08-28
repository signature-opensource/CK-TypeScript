import { AppRoutedComponent } from '../MiscDemo/app-routed/app-routed';
import { PublicPageComponent } from '../Ng/PublicPage/public-page/public-page';
import rPublicPageComponent from '../Ng/PublicPage/public-page/routes';

import { inject } from "@angular/core";
import { DefaultUrlSerializer, Route, Router } from '@angular/router';

let router: Router | null = null;
let urlSerializer: DefaultUrlSerializer | null = null;

export default [
{ path: "app-routed", component: AppRoutedComponent },
{ path: "", component: PublicPageComponent, children: rPublicPageComponent

 },
{
  path: '**',
  redirectTo: () =>
  {
      router ??= inject( Router );
      const currentRoute = router.getCurrentNavigation()?.initialUrl;
      const targetUrl = router.parseUrl( '/' );
      targetUrl.queryParams[ 'notFound' ] = currentRoute
            ? (urlSerializer ??= new DefaultUrlSerializer()).serialize(currentRoute)
            : undefined;
      return targetUrl;
   }
}
] as Route[];