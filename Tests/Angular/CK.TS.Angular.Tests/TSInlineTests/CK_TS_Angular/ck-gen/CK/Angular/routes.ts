import { AppRoutedComponent } from '../MiscDemo/app-routed/app-routed.component';
import { PublicPageComponent } from '../Ng/PublicPage/public-page/public-page.component';
import rPublicPageComponent from '../Ng/PublicPage/public-page/routes';

export default [
{ path: "app-routed", component: AppRoutedComponent },
{ path: "", component: PublicPageComponent, children: rPublicPageComponent

 }
];