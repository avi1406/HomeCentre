import AuthManagerService from '@/services/AuthManagerService';
import { getTheme } from '@fluentui/react';
import { Sidebar } from '@uifabric/experiments';
import { t } from 'i18next';
import { IButtonStyles } from 'office-ui-fabric-react';
import React, { useState } from 'react';
import { useHistory } from 'react-router-dom';
import jwt_decode from 'jwt-decode';

export const AppSidebar = () => {
  const History = useHistory();
  const fontFamily = 'Montserrat, sans-serif';
  const bgcolor = '#008B8B';
  const color = 'White';
  const authManager = new AuthManagerService();
  const token = sessionStorage.getItem('token');
  const decode: any = jwt_decode(token || '');
  const rolelvl: any = decode.RoleLevel;
  const isAuthenticated = authManager.isAuthenticated();
  const user = isAuthenticated ? authManager.getUserData() : null;
  const roleId: any[] = user?.roleId || [];
  const fullPathName = window.location.pathname;
  const pathName = fullPathName.substring(1);
  const customButtonStyles: IButtonStyles = {
    root: {
      backgroundColor: '#008B8B',
    },
    rootHovered: {
      backgroundColor: '#20B2AA',
    },
    rootPressed: {
      backgroundColor: '#20B2AA',
    },
  };
  const [outputClick, setOutputClick] = useState(false);
  return (
    <Sidebar
      className="sidebar_header p "
      style={{ height: '100%', fontFamily: 'Montserrat, sans-serif' }}
      buttonStyles={customButtonStyles}
      collapsible={true}
      theme={getTheme()}
      collapseButtonAriaLabel={'sitemap'}
      items={[
        {
          key: 'dashboard',
          className: 'sidebar_header p',
          name: `${t('MENU_OVERVIEW')}`,
          iconProps: { iconName: 'PieSingle' },
          isVisible: true,
          styles: {
            root: {
              backgroundColor: pathName.includes('overview') ? ' #20B2AA' : `${bgcolor}`,
            },
            label: {
              fontFamily: `${fontFamily}`,
              color: `${color}`,
            },
          },
          onClick: () => {
            History.push('/overview');
            const clicked = 'false';
            sessionStorage.setItem('Tableclicked', clicked);
          },
        },
        {
          key: 'users',
          name: `${t('MENU_USERS')}`,
          className: 'sidebar_header p',
          iconProps: { iconName: 'FabricUserFolder' },
          //active: pathName == 'user',
          isVisible: roleId.indexOf(1) > -1,
          styles: {
            root: {
              backgroundColor: pathName.includes('user') ? ' #20B2AA' : `${bgcolor}`,
            },
            label: {
              fontFamily: `${fontFamily}`,
              color: `${color}`,
            },
          },
          onClick: () => {
            History.push('/user');
            const clicked = 'false';
            sessionStorage.setItem('Tableclicked', clicked);
          },
        },
        {
          key: 'roles',
          className: 'sidebar_header p',
          name: `${t('MENU_ROLES')}`,
          iconProps: { iconName: 'AccountBrowser' },
          //active: pathName == 'roles',
          isVisible: roleId.indexOf(1) > -1,
          styles: {
            root: {
              backgroundColor: pathName.includes('roles') ? ' #20B2AA' : `${bgcolor}`,
            },
            label: {
              fontFamily: `${fontFamily}`,
              color: `${color}`,
            },
          },
          onClick: () => {
            History.push('/roles');
            const clicked = 'false';
            sessionStorage.setItem('Tableclicked', clicked);
          },
        },
        {
          key: 'Dashboard',
          name: `${t('Dashboard')}`,
          iconProps: { iconName: 'BIDashboard' },
          isVisible:
            roleId.indexOf(1) > -1 ||
            roleId.indexOf(2) > -1 ||
            roleId.indexOf(3) > -1 ||
            roleId.indexOf(4) > -1 ||
            roleId.indexOf(5) > -1 ||
            roleId.indexOf(7) > -1,
          styles: {
            root: {
              backgroundColor: pathName.includes('Dashboard') ? ' #20B2AA' : `${bgcolor}`,
            },
            label: {
              fontFamily: `${fontFamily}`,
              color: `${color}`,
            },
          },
          onClick: () => {
            History.push('/Dashboard');
            const clicked = 'false';
            sessionStorage.setItem('Tableclicked', clicked);
          },
        },
        {
          key: 'uidcomparsion',
          name: `${t('MENU_UID_COMPARSION')}`,
          className: 'sidebar_header p',
          iconProps: { iconName: 'BranchCompare' },
          //active: pathName == 'user',
          isVisible: roleId.indexOf(1) > -1,
          styles: {
            root: {
              backgroundColor: pathName.includes('/UidComparsion') ? ' #20B2AA' : `${bgcolor}`,
            },
            label: {
              fontFamily: `${fontFamily}`,
              color: `${color}`,
            },
          },
          onClick: () => {
            History.push('/UidComparsion');
            const clicked = 'false';
            sessionStorage.setItem('Tableclicked', clicked);
          },
        },
        {
          key: 'schematic',
          name: `${t('MENU_SCHEMATIC')}`,
          className: 'sidebar_header p',
          iconProps: { iconName: 'TestPlan' },
          isVisible: true,
          styles: {
            root: {
              backgroundColor: pathName.includes('schematic') ? ' #20B2AA' : `${bgcolor}`,
            },
            label: {
              fontFamily: `${fontFamily}`,
              color: `${color}`,
            },
          },
          items: [
            {
              key: 'createNewSchematic',
              name: `${t('MENU_CREATE_SCHEMATIC')}`,
              className: 'sidebar_header p',
              //active: pathName == 'planogram/CreatePlanogram',
              isVisible: (rolelvl == 'Level 2' && roleId.indexOf(5) > -1) || roleId.indexOf(1) > -1,
              styles: {
                root: {
                  backgroundColor: pathName.includes('schematic/CreateSchematic')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/schematic/CreateSchematic');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
            {
              key: 'approvals',
              name: `${t('MENU_PLANOGRAM_APPROVALS')}`,
              className: 'sidebar_header p',
              //active: pathName == 'planogram/Approvals',
              isVisible: 
              !(roleId.length === 1 && roleId.indexOf(1) > -1) &&
              ( (rolelvl === 'Level 1' && (roleId.indexOf(2) > -1 || roleId.indexOf(5) > -1)) ),
              styles: {
                root: {
                  backgroundColor: pathName.includes('schematic/Approvals')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/schematic/Approvals');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
            {
              key: 'schematicRecords',
              name: `${t('MENU_SCHEMATIC_RECORDS')}`,
              className: 'sidebar_header p',
              //active: pathName == 'planogram/Planogramrecords',
              isVisible: true,
              styles: {
                root: {
                  backgroundColor: pathName.includes('schematic/SchematicRecords')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/schematic/SchematicRecords');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
          ].filter((item) => item.isVisible),
        },
        {
          key: 'DataSync',
          name: `${t('MENU_DATA_SYNC')}`,
          className: 'sidebar_header p',
          iconProps: { iconName: 'AddToShoppingList' },
          isVisible:
          roleId.includes(1) || roleId.includes(7) ||
                    (
              rolelvl === 'Level 1' &&
              roleId.indexOf(5) === -1 &&
              roleId.indexOf(13) === -1 &&
              roleId.indexOf(14) === -1 &&
              roleId.indexOf(16) === -1 &&
              roleId.indexOf(17) === -1 &&
              (
                  (roleId.includes(1) && roleId.includes(2)) ||
                  (roleId.includes(1) && roleId.includes(5))
              )
          ) ||
          (
              rolelvl === 'Level 2' && (roleId.includes(5) || roleId.includes(2))
          ),
          styles: {
            root: {
              backgroundColor: pathName.includes('DataSync') ? ' #20B2AA' : `${bgcolor}`,
            },
            label: {
              fontFamily: `${fontFamily}`,
              color: `${color}`,
            },
          },
          items: [
            {
              key: 'template',
              name: `${t('MENU_DATA_SYNC_TEMPLATE')}`,
              styles: {
                root: {
                  backgroundColor: pathName.includes('DataSync/template')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/DataSync/template');
              },
            },
          ],
        },
        // {
        //   key: 'DataSync',
        //   name: `${t('MENU_DATA_SYNC')}`,
        //   className: 'sidebar_header p',
        //   iconProps: { iconName: 'AddToShoppingList' },
        //   isVisible: rolelvl == 'Level 2' && roleId.indexOf(2) > -1,
        //   styles: {
        //     root: {
        //       backgroundColor: pathName.includes('DataSync') ? ' #20B2AA' : `${bgcolor}`,
        //     },
        //     label: {
        //       fontFamily: `${fontFamily}`,
        //       color: `${color}`,
        //     },
        //   },
        //   items: [
        //     {
        //       key: 'template',
        //       name: `${t('MENU_DATA_SYNC_TEMPLATE')}`,
        //       styles: {
        //         root: {
        //           backgroundColor: pathName.includes('DataSync/template')
        //             ? ' #20B2AA'
        //             : `${bgcolor}`,
        //         },
        //         label: {
        //           fontFamily: `${fontFamily}`,
        //           color: `${color}`,
        //         },
        //       },
        //       onClick: () => {
        //         History.push('/DataSync/template');
        //       },
        //     },
        //   ],
        // },
        {
          key: 'insights',
          name: `${t('MENU_INSIGHTS')}`,
          iconProps: { iconName: 'TextCallout' },
          submenuIconProps: { iconName: 'ChevronDownMed' },
          styles: {
            root: {
              backgroundColor: pathName.includes('insights') ? ' #20B2AA' : `${bgcolor}`,
            },
            label: {
              fontFamily: `${fontFamily}`,
              color: `${color}`,
            },
          },
          isVisible:
            (rolelvl == 'Level 2' && roleId.indexOf(2) > -1) ||
            roleId.indexOf(7) > -1 ||
            roleId.indexOf(11) > -1 ||
            roleId.indexOf(12) > -1 ||
            roleId.indexOf(1) > -1 ||
            roleId.indexOf(13) > -1 ||
            roleId.indexOf(16) > -1 ||
            roleId.indexOf(14) > -1 ||
            (rolelvl == 'Level 2' && roleId.indexOf(5) > -1),
          onClick: () => {
            setOutputClick(!outputClick);
            const clicked = 'false';
            sessionStorage.setItem('Tableclicked', clicked);
          },
          items: [
            {
              key: 'uid',
              name: `${t('UID_RECORDS')}`,
              isVisible: true,
              // roleId.indexOf(14) > -1 == false &&
              // roleId.indexOf(13) > -1 == false &&
              // roleId.indexOf(5) > -1 == true,
              styles: {
                root: {
                  backgroundColor: pathName.includes('insights/uid') ? ' #20B2AA' : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/insights/uid');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
            {
              key: 'itemcode',
              name: `${t('MENU_ITEMCODE')}`,
              className: 'sidebar_header p',
              styles: {
                root: {
                  backgroundColor: pathName.includes('insights/itemcode')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              isVisible:
                // outputClick &&
                (rolelvl == 'Level 2' && roleId.includes(5)) == false ||
                roleId.includes(1) ||
                (rolelvl == 'Level 2' && roleId.includes(2)) ||
                roleId.includes(7) ||
                roleId.includes(11) ||
                roleId.includes(12) ||
                roleId.includes(1) ||
                roleId.includes(13) ||
                roleId.includes(16) ||
                roleId.includes(14),
              // (roleId.indexOf(1) > -1 ||
              //   roleId.indexOf(2) > -1 ||
              //   roleId.indexOf(7) > -1 ||
              //   roleId.indexOf(11) > -1 ||
              //   roleId.indexOf(12) > -1),
              onClick: () => {
                History.push('/insights/itemcode');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
            {
              key: 'template',
              name: `${t('MENU_TEMPLATES')}`,
              className: 'sidebar_header p',
              styles: {
                root: {
                  backgroundColor: pathName.includes('insights/template')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              isVisible:
                // outputClick &&
                (rolelvl == 'Level 2' && roleId.includes(5)) == false ||
                roleId.includes(1) ||
                (rolelvl == 'Level 2' && roleId.includes(2)) ||
                roleId.includes(7) ||
                roleId.includes(11) ||
                roleId.includes(12) ||
                roleId.includes(1) ||
                roleId.includes(13) ||
                roleId.includes(16) ||
                roleId.includes(14),
              // (roleId.indexOf(1) > -1 || roleId.indexOf(13) > -1 || roleId.indexOf(14) > -1),
              onClick: () => {
                History.push('/insights/template');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
            // {
            //   key: 'dataRequest',
            //   name: `${t('MENU_DATA_REQUEST')}`,
            //   className: 'sidebar_header p',
            //   styles: {
            //     root: {
            //       backgroundColor: pathName.includes('insights/datarequest')
            //         ? ' #20B2AA'
            //         : `${bgcolor}`,
            //     },
            //     label: {
            //       fontFamily: `${fontFamily}`,
            //       color: `${color}`,
            //     },
            //   },
            //   isVisible:
            //     outputClick &&
            //     (roleId.indexOf(2) > -1 ||
            //       roleId.indexOf(7) > -1 ||
            //       roleId.indexOf(11) > -1 ||
            //       roleId.indexOf(12) > -1),
            //   onClick: () => {
            //     History.push('/insights/datarequest');
            //     const clicked = 'false';
            //     sessionStorage.setItem('Tableclicked', clicked);
            //   },
            // },
            // {
            //   key: 'qc',
            //   name: `${t('MENU_QC')}`,
            //   className: 'sidebar_header p',
            //   styles: {
            //     root: {
            //       backgroundColor: pathName.includes('insights/qc') ? ' #20B2AA' : `${bgcolor}`,
            //     },
            //     label: {
            //       fontFamily: `${fontFamily}`,
            //       color: `${color}`,
            //     },
            //   },
            //   isVisible:
            //     outputClick &&
            //     (roleId.indexOf(2) > -1 ||
            //       roleId.indexOf(7) > -1 ||
            //       roleId.indexOf(11) > -1 ||
            //       roleId.indexOf(12) > -1),
            //   onClick: () => {
            //     History.push('/insights/qc');
            //     const clicked = 'false';
            //     sessionStorage.setItem('Tableclicked', clicked);
            //   },
            // },
          ].filter((item) => item.isVisible),
        },
        {
          key: 'configuration',
          name: `${t('MENU_CONFIGURATION')}`,
          iconProps: { iconName: 'Product' },
          isVisible: roleId.indexOf(1) > -1,
          styles: {
            root: {
              backgroundColor: pathName.includes('configuration') ? ' #20B2AA' : `${bgcolor}`,
            },
            label: {
              fontFamily: `${fontFamily}`,
              color: `${color}`,
            },
          },
          active: false,
          items: [
            {
              key: 'attributes',
              name: `${t('MENU_ATTRIBUTES')}`,
              // iconProps: { iconName: "AnalyticsView" },
              isVisible: roleId.indexOf(1) > -1,
              styles: {
                root: {
                  backgroundColor: pathName.includes('configuration/createattribute')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/configuration/createattribute');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
            {
              key: 'templates',
              name: `${t('MENU_TEMPLATE')}`,
              // iconProps: { iconName: "PageListSolid" },
              isVisible: roleId.indexOf(1) > -1,
              styles: {
                root: {
                  backgroundColor: pathName.includes('configuration/template')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/configuration/template');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
            {
              key: 'marketPlace',
              name: `${t('MENU_MARKET_PLACE')}`,
              isVisible: roleId.indexOf(1) > -1,
              styles: {
                root: {
                  backgroundColor: pathName.includes('configuration/marketplace')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/configuration/marketplace');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
            {
              key: 'CatalogMapping',
              name: `${t('MENU_CATALOG_MAPPING')}`,
              className: 'sidebar_header p',
              styles: {
                root: {
                  backgroundColor: pathName.includes('configuration/CatalogMapping')
                    ? '#20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              isVisible: roleId.indexOf(1) > -1,
              onClick: () => {
                History.push('/configuration/CatalogMapping');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
            {
              key: 'masterDropdown',
              name: `${t('MASTER_DATA')}`,
              className: 'sidebar_header p',
              // iconProps: { iconName: "MasterDatabase" },
              isVisible: roleId.indexOf(1) > -1,
              styles: {
                root: {
                  backgroundColor: pathName.includes('configuration/masterdata')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/configuration/masterdata');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
            {
              key: 'queryBox',
              name: `${t('MENU_QUERYBOX')}`,
              // iconProps: { iconName: "QueryList" },
              isVisible: roleId.indexOf(14) > -1 == false && roleId.indexOf(13) > -1 == false,
              styles: {
                root: {
                  backgroundColor: pathName.includes('configuration/querybox')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/configuration/querybox');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
            {
              key: 'studioCapacity',
              name: `${t('MENU_STUDIO_CAPACITY')}`,
              // iconProps: { iconName: "QueryList" },
              isVisible: roleId.indexOf(1) > -1,
              styles: {
                root: {
                  backgroundColor: pathName.includes('configuration/studioCapacity')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/configuration/studioCapacity');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
          ].filter((item) => item.isVisible),
        },
        {
          key: 'ReceivedProducts',
          name: `${t('MENU_RECEIVED_PRODUCTSS')}`,
          iconProps: { iconName: 'AnalyticsView' },
          isVisible:
            roleId.indexOf(1) > -1 ||
            roleId.indexOf(7) > -1 ||
            (rolelvl == 'Level 2' && roleId.indexOf(2) > -1)
              ? true
              : false,
          styles: {
            root: {
              backgroundColor: pathName.includes('ReceivedProducts') ? ' #20B2AA' : `${bgcolor}`,
            },
            label: {
              fontFamily: `${fontFamily}`,
              color: `${color}`,
            },
          },
          onClick: () => {
            History.push('/ReceivedProducts');
            const clicked = 'false';
            sessionStorage.setItem('Tableclicked', clicked);
          },
        },
        {
          key: 'photoshoot',
          name: `${t('MENU_PHOTOSHOOTS')}`,
          iconProps: { iconName: 'Photo2Add' },
          isVisible: roleId.indexOf(1) > -1,
          styles: {
            root: {
              backgroundColor: pathName.includes('photoshoot') ? ' #20B2AA' : `${bgcolor}`,
            },
            label: {
              fontFamily: `${fontFamily}`,
              color: `${color}`,
            },
          },
          items: [
            {
              key: 'productToRecords',
              name: `${t('MENU_PRODUCT_TO_RECORDS')}`,
              // iconProps: { iconName: "AnalyticsView" },
              isVisible: roleId.indexOf(14) > -1,
              styles: {
                root: {
                  backgroundColor: pathName.includes('photoshoot/productToRecords')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/photoshoot/productToRecords');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
            {
              key: 'photoShootTracking',
              name: `${t('COL_PHOTOSHOOT_TRACKING')}`,
              // iconProps: { iconName: "AnalyticsView" },
              isVisible: roleId.indexOf(1) > -1 || roleId.indexOf(13) > -1 == false,
              styles: {
                root: {
                  backgroundColor: pathName.includes('photoshoot/photoShootTracking')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/photoshoot/photoShootTracking');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
            {
              key: 'shootpriority',
              name: `${t('SHOOT_PRIORITY')}`,
              // iconProps: { iconName: "AnalyticsView" },
              isVisible: roleId.indexOf(1) > -1 && roleId.indexOf(13) > -1 == true,
              styles: {
                root: {
                  backgroundColor: pathName.includes('photoshoot/shootPriority')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/photoshoot/shootPriority');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
            {
              key: 'CamQCL2',
              name: `${t('PHOTOSHOOT_IMAGE_QC')}`,
              isVisible: roleId.indexOf(1) > -1 && roleId.indexOf(13) > -1 == true,
              styles: {
                root: {
                  backgroundColor: pathName.includes('photoshoot/ShootPriorityL2')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/photoshoot/ShootPriorityL2');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
          ],
        },
        {
          key: 'photoshoot',
          name: `${t('MENU_PHOTOSHOOT')}`,
          iconProps: { iconName: 'Photo2Add' },
          isVisible: rolelvl == 'Level 1' && roleId.indexOf(13) > -1,
          styles: {
            root: {
              backgroundColor: pathName.includes('photoshoot') ? ' #20B2AA' : `${bgcolor}`,
            },
            label: {
              fontFamily: `${fontFamily}`,
              color: `${color}`,
            },
          },
          items: [
            {
              key: 'productToRecords',
              name: `${t('MENU_PRODUCT_TO_RECORDS')}`,
              // iconProps: { iconName: "AnalyticsView" },
              isVisible: roleId.indexOf(14) > -1,
              styles: {
                root: {
                  backgroundColor: pathName.includes('photoshoot/productToRecords')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/photoshoot/productToRecords');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
            {
              key: 'shootpriority',
              name: `${t('SHOOT_PRIORITY')}`,
              styles: {
                root: {
                  backgroundColor: pathName.includes('photoshoot/shootPriority')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/photoshoot/shootPriority');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
          ],
        },

        {
          key: 'photoshoot',
          name: `${t('MENU_PHOTOSHOOT')}`,
          iconProps: { iconName: 'Photo2Add' },
          isVisible: roleId.indexOf(16) > -1,
          styles: {
            root: {
              backgroundColor: pathName.includes('photoshoot') ? ' #20B2AA' : `${bgcolor}`,
            },
            label: {
              fontFamily: `${fontFamily}`,
              color: `${color}`,
            },
          },
          items: [
            {
              key: 'photoShootTracking',
              name: `${t('COL_PHOTOSHOOT_TRACKING')}`,
              isVisible: roleId.indexOf(16) > -1,
              styles: {
                root: {
                  backgroundColor: pathName.includes('photoshoot/photoShootTracking')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/photoshoot/photoShootTracking');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
            // {
            //   key: 'CamQCL2',
            //   name: `${t('PHOTOSHOOT_IMAGE_QC')}`,
            //   isVisible: rolelvl == 'Level 2' && roleId.indexOf(13) > -1,
            //   styles: {
            //     root: {
            //       backgroundColor: pathName.includes('photoshoot/ShootPriorityL2')
            //         ? ' #20B2AA'
            //         : `${bgcolor}`,
            //     },
            //     label: {
            //       fontFamily: `${fontFamily}`,
            //       color: `${color}`,
            //     },
            //   },
            //   onClick: () => {
            //     History.push('/photoshoot/ShootPriorityL2');
            //     const clicked = 'false';
            //     sessionStorage.setItem('Tableclicked', clicked);
            //   },
            // },
          ],
        },
        {
          key: 'photoshoot',
          name: `${t('MENU_PHOTOSHOOT')}`,
          iconProps: { iconName: 'Photo2Add' },
          isVisible: (rolelvl == 'Level 2' && roleId.indexOf(13) > -1),
          styles: {
            root: {
              backgroundColor: pathName.includes('photoshoot') ? ' #20B2AA' : `${bgcolor}`,
            },
            label: {
              fontFamily: `${fontFamily}`,
              color: `${color}`,
            },
          },
          items: [
            // {
            //   key: 'photoShootTracking',
            //   name: `${t('MENU_SHOOTTRACKING')}`,
            //   isVisible: roleId.indexOf(16) > -1,
            //   styles: {
            //     root: {
            //       backgroundColor: pathName.includes('photoshoot/photoShootTracking')
            //         ? ' #20B2AA'
            //         : `${bgcolor}`,
            //     },
            //     label: {
            //       fontFamily: `${fontFamily}`,
            //       color: `${color}`,
            //     },
            //   },
            //   onClick: () => {
            //     History.push('/photoshoot/photoShootTracking');
            //     const clicked = 'false';
            //     sessionStorage.setItem('Tableclicked', clicked);
            //   },
            // },
            {
              key: 'CamQCL2',
              name: `${t('PHOTOSHOOT_IMAGE_QC')}`,
              isVisible: rolelvl == 'Level 2' && roleId.indexOf(13) > -1,
              styles: {
                root: {
                  backgroundColor: pathName.includes('photoshoot/ShootPriorityL2')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/photoshoot/ShootPriorityL2');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            }
          ],
        },
        {
          key: 'photoshoot',
          name: `${t('MENU_PHOTOSHOOT')}`,
          iconProps: { iconName: 'Photo2Add' },
          isVisible: roleId.indexOf(14) > -1,
          styles: {
            root: {
              backgroundColor: pathName.includes('photoshoot') ? ' #20B2AA' : `${bgcolor}`,
            },
            label: {
              fontFamily: `${fontFamily}`,
              color: `${color}`,
            },
          },
          items: [
            {
              key: 'productToRecords',
              name: `${t('MENU_PRODUCT_TO_RECORDS')}`,

              styles: {
                root: {
                  backgroundColor: pathName.includes('photoshoot/productToRecords')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/photoshoot/productToRecords');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
          ],
        },
        {
          key: 'photoshoot',
          name: `${t('MENU_PHOTOSHOOT')}`,
          iconProps: { iconName: 'Photo2Add' },
          isVisible: (rolelvl === 'Level 2' && (roleId.indexOf(2) > -1)),
          styles: {
            root: {
              backgroundColor: pathName.includes('photoshoot') ? '#20B2AA' : `${bgcolor}`,
            },
            label: {
              fontFamily: `${fontFamily}`,
              color: `${color}`,
            },
          },
          items: [
            {
              key: 'productToRecords',
              name: `${t('MENU_PRODUCT_TO_RECORDS')}`,
              styles: {
                root: {
                  backgroundColor: pathName.includes('photoshoot/productToRecords')
                    ? '#20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/photoshoot/productToRecords');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
          ],
        },
        {
          key: 'content',
          name: `${t('MENU_CONTENT')}`,
          className: 'sidebar_header p',
          iconProps: { iconName: 'ExploreContent' },
          isVisible: roleId.indexOf(1) > -1 || roleId.indexOf(96) > -1 || roleId.indexOf(17) > -1,
          styles: {
            root: {
              backgroundColor: pathName.includes('Content') ? ' #20B2AA' : `${bgcolor}`,
            },
            label: {
              fontFamily: `${fontFamily}`,
              color: `${color}`,
            },
          },
          items: [
            {
              key: 'template',
              name: `${t('CONTENT_SHOOTTRACKING')}`,
              isVisible: roleId.indexOf(96) > -1,
              styles: {
                root: {
                  backgroundColor: pathName.includes('Content/ShootTracking')
                    ? ' #20B2AA'
                    : `${bgcolor}`,
                },
                label: {
                  fontFamily: `${fontFamily}`,
                  color: `${color}`,
                },
              },
              onClick: () => {
                History.push('/Content/ShootTracking');
                const clicked = 'false';
                sessionStorage.setItem('Tableclicked', clicked);
              },
            },
          ],
        },

        // {
        //   key: 'item-5',
        //   name: `${user?.firstName}`,
        //   className: 'sidebar_header p',
        //   iconProps: { iconName: 'More' },
        //   submenuIconProps: { iconName: 'ChevronDownMed' },
        //   isVisible: true,
        //   // active: false,
        //   styles: {
        //     root: {
        //       backgroundColor: pathName.includes('logout') ? ' #20B2AA' : `${bgcolor}`,
        //     },
        //     label: {
        //       fontFamily: `${fontFamily}`,
        //       color: `${color}`,
        //     },
        //   },
        //   items: [
        //     {
        //       key: 'useritem-2',
        //       name: `${t('MENU_LOGOUT')}`,
        //       iconProps: { iconName: 'SignOut' },
        //       className: 'sidebar_header p',
        //       //active: pathName == 'logout',
        //       styles: {
        //         root: {
        //           backgroundColor: pathName.includes('logout') ? `${bgcolor}` : ' #20B2AA',
        //         },
        //         label: {
        //           fontFamily: `${fontFamily}`,
        //           color: `${color}`,
        //         },
        //       },
        //       onClick: () => {
        //         History.push('/logout');
        //       },
        //     },
        //   ],
        // },
      ].filter((item) => item.isVisible)}
    />
  );
};
