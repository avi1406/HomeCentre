import { Route } from 'react-router-dom';
import React from 'react';
import AppHeader from './AppHeader';
import Dashboard from '@/pages/Dashboard';
import Login from '@/pages/Login';
import Logout from '@/pages/Logout';
import Overview from '@/pages/Overview';
import ParseToken from '@/pages/ParseToken';
import { AppSidebar } from './AppSidebar';
import PlanogramCommon from '../pages/PlanoGram/PlanogramCommon';
import CreatePlanogram from '@/pages/PlanoGram/CreatePlanogram';
import Templates from '../pages/Configuration/Templates/templates';
import Data from '@/pages/Input/Data';
import Notificationss from '@/pages/Notificationss';
import CreateTemplateAttribute from '@/pages/Configuration/Templates/createTemplateAttribute';
import FileData from '@/pages/Input/fileData';
import CreateAttribute from '@/pages/Configuration/Attributes/createAttribute';
import UID from '@/pages/Input/uid';
import TableTemplate from '@/pages/Output/OutputTemplate/TableTemplate';
import OutputTemplateAttribute from '@/pages/Output/OutputTemplate/outputTemplateAttribute';
import MarketPlace from '../pages/Configuration/Market Place/marketPlace';
import MarketAttribute from '../pages/Configuration/Market Place/marketAttribute';
import DataRequest from '../pages/Output/DataRequest/DataRequestComponent';
import QueryBox from '../pages/Configuration/QueryBox/query';
import DRviewData from '@/pages/Output/DataRequest/DRviewData';
import Approvals from '../pages/PlanoGram/Approvals';
import ItemCode from '../pages/Output/itemCode';
import QC from '../pages/Output/QC/qc';
import Role from '@/pages/UserDetails/Role';
import User from '@/pages/UserDetails/User';
import Masterdata from '../pages/Configuration/MasterData';
import CatalogMapping from '@/pages/Configuration/CatalogMapping';
import ReceivedProducts from '@/pages/ReceivedProduct/ReceivedProducts';
import StudioCapacity from '@/pages/Configuration/Studio/StudioCapacity';
import ProductToRecords from '@/pages/PhotoShoot/ProductToRecords';
import SHOOTPRIORITY from '@/pages/PhotoShoot/ShootPriority';
import SHOOTTRACKING from '@/pages/Content/ShootTracking';
import CamQCL2 from '@/pages/PhotoShoot/CamQCL2';
import UidComparsion from '@/pages/UidComparsion/UidComparsion';
import CrtPhotoShootTracking from '@/pages/PhotoShoot/CrtPhotoShootTracking';
import CrtShootPriority from '@/pages/PhotoShoot/CrtShootPriority';
import CrtCamQCL2 from '@/pages/PhotoShoot/CrtCamQCL2';
import DashboardPlanogram from '@/pages/Dashboard/DashboardPlanogram';

class AppLayout extends React.Component<any, any> {
  render() {
    const isAuthenticated = this.props.isAuthenticated;
    const user = this.props.user;
    const userrole: any[] = user?.roleId;
    const notDesigner = !(
      userrole?.filter((element: any) => element == 5 || element == 4).length > 0
    );
    return (
      <>
        <div className="parent">
          {isAuthenticated && <AppHeader />}
          <div className="main">
            {isAuthenticated && user && <AppSidebar />}
            <div className="appcontent content">
              <Route exact path="/" component={Login} />
              <Route exact path="/login" component={Login} />
              <Route
                exact
                path="/overview"
                component={
                  userrole?.indexOf(1) > -1
                    ? Overview
                    : userrole?.indexOf(1) > -1 == false && userrole != undefined
                    ? Overview
                    : Logout
                }
              />
              <Route
                exact
                path="/roles"
                component={userrole?.indexOf(1) > -1 || userrole?.indexOf(10) > -1 ? Role : Logout}
              />
              <Route
                exact
                path="/user"
                component={userrole?.indexOf(1) > -1 || userrole?.indexOf(10) > -1 ? User : Logout}
              />
              <Route
                exact
                path="/Dashboard"
                component={isAuthenticated ? DashboardPlanogram : Logout}
              />
              <Route
                exact
                path="/uidcomparsion"
                component={
                  userrole?.indexOf(1) > -1 || userrole?.indexOf(10) > -1 ? UidComparsion : Logout
                }
              />
              <Route
                exact
                path="/schematic/CreateSchematic"
                component={isAuthenticated ? CreatePlanogram : Logout}
              />
              <Route
                exact
                path="/schematic/Approvals"
                component={isAuthenticated ? Approvals : Logout}
              />
              <Route
                exact
                path="/schematic/SchematicRecords"
                component={isAuthenticated ? PlanogramCommon : Logout}
              />

              <Route exact path="/DataSync/template" component={isAuthenticated ? Data : Logout} />
              <Route exact path="/insights/uid" component={isAuthenticated ? UID : Logout} />
              <Route
                exact
                path="/DataSync/template/filedata/*"
                component={isAuthenticated ? FileData : Logout}
              />

              <Route
                exact
                path="/input/template/filedata/*"
                component={isAuthenticated ? FileData : Logout}
              />

              <Route
                exact
                path="/insights/itemcode"
                component={isAuthenticated ? ItemCode : Logout}
              />
              <Route
                exact
                path="/insights/template"
                component={isAuthenticated ? TableTemplate : Logout}
              />
              <Route
                exact
                path="/configuration/CatalogMapping/templateAttibute/*"
                component={isAuthenticated ? OutputTemplateAttribute : Logout}
              />
              {/* <Route exact path="/insights/qc" component={isAuthenticated ? QC : Logout} /> */}
              {/* <Route
                exact
                path="/insights/datarequest"
                component={isAuthenticated ? DataRequest : Logout}
              /> */}
              {/* <Route
                exact
                path="/insights/datarequest/viewdata/*"
                component={isAuthenticated ? DRviewData : Logout}
              /> */}
              <Route
                exact
                path="/configuration/Notificationss"
                component={isAuthenticated ? Notificationss : Logout}
              />

              <Route
                exact
                path="/configuration/template"
                component={isAuthenticated && userrole?.indexOf(1) > -1 ? Templates : Logout}
              />
              <Route
                exact
                path="/configuration/createattribute"
                component={isAuthenticated && userrole?.indexOf(1) > -1 ? CreateAttribute : Logout}
              />
              <Route
                exact
                path="/configuration/template/addattribute/*"
                component={
                  isAuthenticated && userrole?.indexOf(1) > -1 ? CreateTemplateAttribute : Logout
                }
              />
              <Route
                exact
                path="/configuration/marketplace"
                component={isAuthenticated && userrole?.indexOf(1) > -1 ? MarketPlace : Logout}
              />
              <Route
                exact
                path="/configuration/CatalogMapping"
                component={isAuthenticated && userrole?.indexOf(1) > -1 ? CatalogMapping : Logout}
              />
              <Route
                exact
                path="/configuration/marketplace/marketattribute/*"
                component={isAuthenticated && userrole?.indexOf(1) > -1 ? MarketAttribute : Logout}
              />
              <Route
                exact
                path="/configuration/masterdata"
                component={isAuthenticated && userrole?.indexOf(1) > -1 ? Masterdata : Logout}
              />
              <Route
                exact
                path="/configuration/querybox"
                component={isAuthenticated && userrole?.indexOf(1) > -1 ? QueryBox : Logout}
              />
              <Route
                exact
                path="/configuration/studioCapacity"
                component={isAuthenticated && userrole?.indexOf(1) > -1 ? StudioCapacity : Logout}
              />

              <Route
                exact
                path="/ReceivedProducts"
                component={isAuthenticated ? ReceivedProducts : Logout}
              />

              <Route
                exact
                path="/photoshoot/productToRecords"
                component={isAuthenticated ? ProductToRecords : Logout}
              />

              <Route
                exact
                path="/photoshoot/photoShootTracking"
                component={isAuthenticated ? CrtPhotoShootTracking : Logout}
              />

              <Route
                exact
                path="/photoshoot/shootPriority"
                component={isAuthenticated ? CrtShootPriority : Logout}
              />
              <Route
                exact
                path="/photoshoot/ShootPriorityL2"
                component={isAuthenticated ? CrtCamQCL2 : Logout}
              />

              <Route
                exact
                path="/Content/ShootTracking"
                component={isAuthenticated ? SHOOTTRACKING : Logout}
              />

              <Route exact path="/logout" component={Logout} />
              <Route path="/parsetoken" component={ParseToken} />
            </div>
          </div>
        </div>
      </>
    );
  }
}
export default AppLayout;
