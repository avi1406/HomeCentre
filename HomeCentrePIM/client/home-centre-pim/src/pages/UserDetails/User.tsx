import React, { Suspense } from 'react';
import 'react-phone-input-2/lib/style.css';
import { SearchBox } from '@fluentui/react/lib/SearchBox';
import DataTable from 'react-data-table-component';
import { DefaultButton, IconButton } from '@fluentui/react/lib/Button';
import { Dropdown, IDropdownOption } from 'office-ui-fabric-react/lib/Dropdown';
import { ITextProps, Text } from '@fluentui/react/lib/Text';
import { Icon, Modal, Stack } from '@fluentui/react';
import ApiManager from '@/services/ApiManager';
import 'office-ui-fabric-react/dist/css/fabric.css';
import {
  cancelButton,
  cancelIcon,
  iconButtonStyles,
  itemAlignmentsStackStyles,
  itemAlignmentsStackTokens,
  AssigndropdownStyles,
  CancelButtonStyles,
  UserMarketplaceStyles,
  UploadStyles,
  departmentSelect,
} from '@/common/properties';
import { contentStyles } from '@/common/Modal';
import { withTranslation } from 'react-i18next';
import { Grid } from '@mui/material';
import { AppuserMarketplace, AppUserRole } from '@/services/ApiClient';
import NotificationManager from '@/services/NotificationManager';
import axios from 'axios';
import { baseUrl } from '@/services/Constants';
import AuthManagerService from '@/services/AuthManagerService';
import jwt_decode from 'jwt-decode';
import {
  activeText,
  columnHeader,
  hyperlink,
  PIMButtons,
  PIMcontentStyles,
  PIMdropdownStyles,
  PIMfooterButtonCancel,
  PIMfooterButtonConfirm,
  PIMHearderText,
  PIMsearchBoxStyles,
} from '../PimStyles';
import AddorUpdateUser from './AddorUpadateUser';

class User extends React.Component<any, any> {
  authManager = new AuthManagerService();
  apiClient = new ApiManager().CreateApiClient();
  token = sessionStorage.getItem('token');
  decode: any = jwt_decode(this.token || '');
  roleId: any = this.decode.RoleId.split(',').map(Number);
  options: IDropdownOption[] = [
    { key: 2, text: 'SelectAll' },
    { key: 0, text: 'Yes' },
    { key: 1, text: 'No' },
  ];
  notify = new NotificationManager();

  constructor(props: any) {
    super(props);
    this.state = {
      A_D_Visible: false,
      A_D_Visible1: false,
      currentRecord: [],
      enableEditButton: false,
      enableActiveButton: false,
      inActivePage: false,
      rolevisible: false,
      copyRecord: [],
      searchKey: '',
      roleList: [],
      selectedRoleIds: [],
      selectedRowData: [],
      roleOptions: [],
      noSelection: false,
      isAssignDepartment: false,
      departmentList: [],
      setDropdown: false,
      divisionOption: [],
      isRecord: true,
      assignedMarketPlaceData: [],
      MarketPlaceList: [],
      filtervalue: 1,
      selectedRowData1: [],
      visible: false,
      divisionSelected: '',
    };
  }

  componentDidMount() {
    this.refresh(2);
  }

  // async refresh(inActivePage: number) {
  //   try {
  //     this.setState({ searchKey: '', isRecord: true, inActivePage:inActivePage, });
  //     const rolename: any = [];
  //     const roleList: any = [];
  //     const departmentList: any = [];
  //     const MarketPlaceList: any = [];
  //     const apiResponse: any = (await this.apiClient.searchAllUsers(inActivePage)).result?.records;
  //     const role: any = (await this.apiClient.getRoleName()).result;
  //     const studioName: any = (await this.apiClient.getAllUserStudioDetails()).result;
  //     this.apiClient.getRoles(true).then((r: any) => {
  //       r.result.forEach((element: any) => {
  //         roleList.push({ key: element.id, text: element.name });
  //       });
  //     });
  //     this.apiClient.searchMarketplaces(undefined, 0, 0, true).then((response: any) => {
  //       response.result.records.forEach((element: any) => {
  //         MarketPlaceList.push({ key: element.id, text: element.name });
  //       });
  //     });
  //     this.apiClient.getDivision().then((r: any) => {
  //       r.result.forEach((element: any) => {
  //         departmentList.push({ key: element.id, text: element.name });
  //       });
  //     });
  //     this.setState({
  //       currentRecord: apiResponse?.map((item: any, index: any) => ({ index: index + 1, ...item })),
  //       copyRecord: apiResponse?.map((item: any, index: any) => ({ index: index + 1, ...item })),
  //       roleList: roleList,
  //       rolename: role,
  //       studioName: studioName,
  //       departmentList: departmentList,
  //       MarketPlaceList: MarketPlaceList,
  //       noSelection: !this.state.noSelection,
  //     });
  //   } catch (e: any) {
  //     this.notify.showErrorNotify(e.message);
  //   }
  //   this.setState({
  //     A_D_Visible: false,
  //     noSelection: !this.state.noSelection,
  //     enableEditButton: false,
  //     enableActiveButton: false,
  //     selectedRowData: [],
  //     isRecord: false,
  //   });
  // }
  async refresh(inActivePage: number) {
    try {
      this.setState({ searchKey: '', isRecord: true, inActivePage: inActivePage });
      const apiResponsePromise = this.apiClient.searchAllUsers(inActivePage);
      const rolePromise = this.apiClient.getRoleName();
      const studioNamePromise = this.apiClient.getAllUserStudioDetails();
      const rolesPromise = this.apiClient.getRoles(true);
      const divisionPromise = this.apiClient.getDivision();
      const [
        apiResponse,
        role,
        studioName,
        roles,
        division,
      ] = await Promise.all([
        apiResponsePromise,
        rolePromise,
        studioNamePromise,
        rolesPromise,
        divisionPromise,
      ]);
  
      const roleList = roles?.result?.map((element: any) => ({
        key: element.id,
        text: element.name,
      }));
  
      const departmentList = division?.result?.map((element: any) => ({
        key: element.id,
        text: element.name,
      }));
      this.setState({
        currentRecord: apiResponse.result?.records?.map((item: any, index: any) => ({ index: index + 1, ...item })),
        copyRecord: apiResponse.result?.records?.map((item: any, index: any) => ({ index: index + 1, ...item })),
        roleList: roleList,
        rolename: role.result,
        studioName: studioName.result,
        departmentList: departmentList,
        noSelection: !this.state.noSelection,
      });
    } catch (e: any) {
      this.notify.showErrorNotify(e.message);
    } finally {
      this.setState({
        A_D_Visible: false,
        noSelection: !this.state.noSelection,
        enableEditButton: false,
        enableActiveButton: false,
        selectedRowData: [],
        isRecord: false,
      });
    }
  }
  perpage = () => {
    const recordCont = [25, 50, 100, 150, 200];
    return recordCont;
  };
  Defaultperpage = () => {
    const recordCont = 25;
    return recordCont;
  };
  DeleteUserRoles = async (row: any) => {
    var Body = new AppUserRole();
    Body.appUserId = this.state.selectedRowData[0].id;
    Body.departmentId = row.id;
    Body.name = '';
    Body.description = '';
    const response = this.state.isAssignDepartment
      ? await this.apiClient.deleteDepartmentToUser(Body)
      : await this.apiClient.deleteRoles(row.appuserid, row.roleid);
    this.onAssign(this.state.isAssignDepartment);
    if (response.hasError) {
      this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN'));
    } else {
      this.notify.showSuccessNotify(this.props.t('MSG_DEL'));
    }
  };

  DeleteUserMarketPlace = async (row: any) => {
    const Ids: any = [];
    Ids.push(row.id);
    const response = await this.apiClient.unlinkUserFromMarketPlaces(Ids);
    if (response.hasError) {
      this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN'));
    } else {
      this.notify.showSuccessNotify(this.props.t('MSG_DEL_MARKETPLACE'));
    }
    this.assignMarrketPlace();
  };

  rowSelection = (e: any) => {
    this.setState({ selectedRowData: e.selectedRows });
    const selectionCount = e.selectedCount;

    switch (selectionCount) {
      case 0:
        this.setState({ enableEditButton: false, enableActiveButton: false });
        break;
      case 1:
        this.setState({ enableEditButton: true, enableActiveButton: true });
        break;
      default:
        this.setState({ enableEditButton: false, enableActiveButton: true });
        break;
    }
  };

  downloadFile = () => {
    this.notify.showSuccessNotify1(this.props.t('DOWNLOAD_STARTED'));
    document.body.classList.add('loading-indicator');

    const { inActivePage } = this.state;

    const _url =
      inActivePage === 0
        ? `${baseUrl}/api/User/UserExportExcel?isActive=true`
        : inActivePage === 1
        ? `${baseUrl}/api/User/UserExportExcel?isActive=false`
        : `${baseUrl}/api/User/UserExportExcel`;

    axios({
      method: 'POST',
      url: _url,
      responseType: 'blob',
      headers: {
        Token: this.token,
        'Content-Type': 'application/json;',
      },
    })
      .then((response: any) => {
        document.body.classList.remove('loading-indicator');
        this.notify.hideErrorNotify();
        this.notify.showSuccessNotify(this.props.t('DOWNLOAD_COMPLETED'));

        const url = window.URL.createObjectURL(new Blob([response.data]));
        const link = document.createElement('a');
        link.href = url;
        link.setAttribute('download', 'UserMasterExcel.xlsx');
        link.click();

        window.URL.revokeObjectURL(url);
      })
      .catch((error: any) => {
        document.body.classList.remove('loading-indicator');
        console.error('Error:', error);
        this.notify.showErrorNotify(error);
      });
  };

  async onActiaveClick() {
    const selectedRecordIds = [];
    for (let i = 0; i < this.state.selectedRowData.length; i++) {
      selectedRecordIds.push(this.state.selectedRowData[i].id);
    }
    const allActive = this.state.selectedRowData.every((row:any) => !row.isActive);

    const response = this.state.inActivePage && allActive
      ? await this.apiClient.activateUserBatch(selectedRecordIds)
      : await this.apiClient.deactivateUserBatch(selectedRecordIds);
    if (response.hasError) {
      this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN'));
    } else {
      this.notify.showSuccessNotify(
        this.state.inActivePage && allActive
          ? `${this.props.t('MSG_ACTIVATE')}`
          : `${this.props.t('MSG_DEACTIVATE')}`,
      );
    }
    this.setState({divisionSelected:''})
    this.refresh(2);
  }

  addUserRole = async () => {
    const data: any = {
      appUserid: this.state.selectedRowData[0].id,
      appUserRoleDomainModel: this.state.selectedRoleIds,
    };
    const response = this.state.isAssignDepartment
      ? await this.apiClient.addDepartmentToUser(data)
      : await this.apiClient.addUserRole(data);
    if (response.hasError) {
      this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN'));
    } else {
      this.notify.showSuccessNotify(
        this.state.isAssignDepartment
          ? `${this.props.t('MSG_DEPARTMENT_ASSIGN')}`
          : `${this.props.t('MSG_ASSIGN')}`,
      );
    }
    this.onAssign(this.state.isAssignDepartment);
  };

  AddMarketPlaceToUser = async () => {
    var body: AppuserMarketplace[] = [];
    var { selectedRoleIds } = this.state;
    selectedRoleIds?.forEach((element: any) => {
      body.push(
        new AppuserMarketplace({
          appUserId: this.state.selectedRowData[0]?.id,
          marketplaceId: element.id,
        }),
      );
    });
    var response = await this.apiClient.linkUsersToMarketPlaces(body);
    if (response.hasError) {
      this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN'));
    } else {
      this.notify.showSuccessNotify(`${this.props.t('MSG_MARKETPLACE_ASSIGN')}`);
    }
    (error: any) => {
      this.notify.showErrorNotify(error);
    };
    this.assignMarrketPlace();
    this.setState({ selectedRoleIds: [] });
  };

  onAssign = async (isDepartment: boolean) => {
    this.setState({ isAssignDepartment: isDepartment });
    var filteredArray;
    const result = isDepartment
      ? (await this.apiClient.getUserDepartment(this.state.selectedRowData[0].id)).result
      : (await this.apiClient.getUserRoles(this.state.selectedRowData[0].id)).result;
    if (isDepartment) {
      this.setState({ setDropdown: false });
      filteredArray = result?.length == 0 ? this.state.departmentList : [];
    } else {
      filteredArray = this.state.roleList.filter(function (roleList_el: any) {
        return (
          result?.filter(function (userRole_el: any) {
            return userRole_el.roleid == roleList_el.key;
          }).length == 0
        );
      });
    }
    this.setState({
      roledata: result,
      rolevisible: true,
      roleOptions: filteredArray,
      selectedRoleIds: [],
    });
  };

  assignMarrketPlace = async () => {
    var filteredArray;
    const result = (await this.apiClient.getMarketplacesOfUser(this.state.selectedRowData[0].id))
      .result;
    var data: any = [];
    var uniqueArray = result?.filter((element) => {
      var match = data.filter((elements: any) => elements.id == element.id);
      if (match.length == 0) {
        data.push(element);
        return true;
      }
      return false;
    });
    filteredArray = this.state.MarketPlaceList?.filter(
      (element: any) => data.filter((el: any) => el.marketplaceId == element.key).length == 0,
    );
    this.setState({
      assignedMarketPlaceData: data,
      masterplaceVisible: true,
      MarketAssignOption: filteredArray,
    });
  };

  MarketOptionChange(event: any) {
    var { selectedRoleIds } = this.state;
    var selectedMarketPlace = [];
    if (event.selected) {
      selectedMarketPlace = [...selectedRoleIds, { id: event.key }];
    } else {
      selectedMarketPlace = selectedRoleIds?.filter((element: any) => element.id != event.key);
    }
    this.setState({ selectedRoleIds: selectedMarketPlace });
  }

  onSearch(e: any) {
    var newValue = e?.target.value;
    newValue = newValue == undefined ? '' : newValue;
    this.setState({ searchKey: newValue });
    var { copyRecord } = this.state;
    var result = copyRecord.filter((element: any) => {
      return (
        element?.name
          ?.toLowerCase()
          .includes(newValue.trim().split(/ +/).join(' ').toLowerCase()) ||
        element?.email?.toLowerCase().includes(newValue.toLowerCase()) ||
        element?.mobile?.includes(newValue.toLowerCase())
      );
    });
    this.setState({ currentRecord: result });
  }

  onRoleChange(event: any) {
    let selectedRole = [];
    if (this.state.isAssignDepartment) {
      selectedRole = [{ roleId: 0, departmentId: event.key, name: event.text }];
    } else {
      if (event.selected) {
        selectedRole = [
          ...this.state.selectedRoleIds,
          {
            roleId: this.state.isAssignDepartment ? 0 : event.key,
            departmentId: this.state.isAssignDepartment ? event.key : 0,
            name: event.text,
          },
        ];
      } else {
        selectedRole = this.state.selectedRoleIds.filter(function (value: any) {
          return value.roleId != event.key;
        });
      }
    }
    this.setState({
      selectedRoleIds: selectedRole,
      enableEditButton: false,
      enableActiveButton: false,
    });
  }
  handleRowClick = (row: any) => {
    const roleLevel = row.rolelevel || '';
    const levelsArray = roleLevel.split(', ');
    const transformedArray = levelsArray.map((item: any) => ({
      key: item,
      text: item,
    }));
    this.setState({
      visible: true,
      selectedRowData: [row],
      selectableRowskk: transformedArray,
    });
  };
  DeleteUser = async () => {
    this.setState({ A_D_Visible1: false });
    try {
      if (this.state.selectedRowData && this.state.selectedRowData.length > 0) {
        const ids = this.state.selectedRowData.map((e: any) => parseInt(e.id, 10));
        await this.apiClient.deleteUser(ids);
        this.notify.showSuccessNotify('Deleted successfully');
        this.refresh(2);
      }
    } catch (error) {
      console.error('Error deleting user:', error);
    }
    this.setState({divisionSelected:''})
    this.refresh(2);
  };
  handleDropdownChange = (
    event: React.FormEvent<HTMLDivElement>,
    option?: IDropdownOption,
    index?: number,
  ) => {
    const { options } = this;
    if (option) {
      let selectedKeys = [...this.state.divisionSelected];

      if (option.key === 2) {
        if (option.selected) {
          selectedKeys = options.map((opt: IDropdownOption) => opt.key as string);
        } else {
          selectedKeys = [];
        }
      } else {
        if (option.selected) {
          if (!selectedKeys.includes(option.key as string)) {
            selectedKeys.push(option.key as string);
          }
        } else {
          selectedKeys = selectedKeys.filter((key) => key !== (option.key as string));
        }

        if (selectedKeys.includes(0) && selectedKeys.includes(1)) {
          if (!selectedKeys.includes(2)) {
            selectedKeys.push(2);
          }
        } else if (!selectedKeys.includes(0) || !selectedKeys.includes(1)) {
          selectedKeys = selectedKeys.filter((key) => key !== 2);
        }
      }

      const allOptionsSelected = options.length > 0 && selectedKeys.length === options.length;
      if (allOptionsSelected) {
        if (!selectedKeys.includes(2)) {
          selectedKeys = [2, ...selectedKeys];
        }
      } else {
        selectedKeys = selectedKeys.filter((key) => key !== 2);
      }

      this.setState(
        {
          divisionSelected: selectedKeys,
          inActivePage: this.state.inActivePage,
          currentPage: 1,
          noSelection: true,
          filtervalue: this.state.inActivePage === 1 ? 0 : 1,
        },
        () => {
          const statusdropdown = selectedKeys.join(',');
          this.setState({ statusdropdown }, () => {
            const inActivePage = selectedKeys?.includes(2)
              ? 2
              : selectedKeys?.length === 1
              ? selectedKeys[0]
              : 2;
            this.refresh(inActivePage);
          });
        },
      );
    }
  };

  render() {
    const { currentRecord } = this.state;
    const { t } = this.props;
    const columns: any = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{t('S_NO')}</div>,
        selector: (row: any) => row.index,
        width: '70px',
      },
      {
        key: 'nameColumn',
        name: <div className={columnHeader}>{t('TABLE_DESCRIPTION_NAME')}</div>,
        selector: (row: any) => row.name,
        cell: (row: any) => (
          <span className={hyperlink} onClick={() => this.handleRowClick(row)}>
            {row.name}
          </span>
        ),
        width: '200px',
      },
      {
        key: 'emailColumn',
        name: <div className={columnHeader}>{t('TABLE_DESCRIPTION_EMAIL')}</div>,
        selector: (row: any) => <span title={row.email}>{row.email}</span>,
        width: '220px',
      },
      {
        key: 'mobileColumn',
        name: <div className={columnHeader}>{t('TABLE_DESCRIPTION_MOBILE')}</div>,
        selector: (row: any) => <span title={row.mobile}>{row.mobile}</span>,
        width: '120px',
      },
      {
        key: 'deptName',
        name: <div className={columnHeader}>{t('TABLE_DESCRIPTION_IS_DIVISION')}</div>,
        selector: (row: any) => {
          if (this.state.rolename && this.state.rolename.length > 0) {
            const roles = this.state.rolename.filter((e: any) => {
              return row.id === e.appUserId;
            });
            const roleNames = roles.map((role: any) => role.departmentName);
            const departments = roleNames.filter((f: any) => f !== null);
            return <span title={departments.join(', ')}>{departments.join(', ')}</span>;
          } else {
            return '';
          }
        },
        width: '200px',
      },
      {
        key: 'StudioName',
        name: <div className={columnHeader}>{t('USER_BTN_STUDIO')}</div>,
        selector: (row: any) => {
          if (this.state.studioName && this.state.studioName.length > 0) {
            const roles = this.state.studioName.filter((e: any) => {
              return row.id === e.userId;
            });
            const roleNames = roles.map((role: any) => role.studioName);
            const departments = roleNames.join(', ');
            return <span title={departments}>{departments}</span>;
          } else {
            return '';
          }
        },
        width: '200px',
      },
      {
        key: 'roleName',
        name: <div className={columnHeader}>{t('USER_BTN_ASSIGN_ROLES')}</div>,
        selector: (row: any) => {
          if (this.state.rolename && this.state.rolename.length > 0) {
            const roles = this.state.rolename.filter((e: any) => {
              if (e.roleId != 6) {
                return row.id === e.appUserId;
              }
            });
            const roleNames = roles.map((role: any) => role.name);
            return <span title={roleNames.join(', ')}>{roleNames.join(', ')}</span>;
          } else {
            return '';
          }
        },
        width: '200px',
      },
      {
        key: 'roleLevel',
        name: <div className={columnHeader}>{t('ROLE_LEVEL')}</div>,
        selector: (row: any) => <span title={row.rolelevel}>{row.rolelevel}</span>,
        width: '120px',
      },
      {
        key: 'isActive',
        name: <div className={columnHeader}>{t('COL_ISACTIVE')}</div>,
        selector: (row: any) => <span title={row.isActive}>{row.isActive}</span>,
        cell: (row: any) =>
          this.state.currentRecord.length !== 0 && (
            <span title={String(row.isActive)}>
              {row.isActive ? this.props.t('COL_YES') : this.props.t('COL_NO')}
            </span>
          ),
        width: '120px',
      },
    ];
    const roleColumn: any = [
      {
        key: 'name',
        name: <div className={columnHeader}>{t('TABLE_DESCRIPTION_NAME')}</div>,
        selector: (row: any) => row.name,
      },
      {
        key: 'roleId',
        name: <div className={columnHeader}>{t('ID')}</div>,
        selector: (row: any) => row.roleid || row.id,
      },
      {
        key: 'action',
        name: '',
        selector: (row: any) => (
          <DefaultButton
            className="button"
            onClick={() => this.DeleteUserRoles(row)}
            iconProps={{ iconName: 'Delete' }}
            styles={cancelButton}
            text={t('BTN_DELETE')}
          />
        ),
      },
    ];
    const MarketPlaceColumn: any = [
      {
        key: 'name',
        name: <div className={columnHeader}>{t('TABLE_DESCRIPTION_NAME')}</div>,
        selector: (row: any) => row.name,
      },
      {
        key: 'id',
        name: <div className={columnHeader}>{t('MARKET_PLACE_ID')}</div>,
        selector: (row: any) => row.name,
      },
      {
        key: 'action',
        selector: (row: any) => (
          <DefaultButton
            className="button"
            iconProps={{ iconName: 'Delete' }}
            styles={cancelButton}
            text={this.props.t('BTN_DELETE')}
            onClick={() => this.DeleteUserMarketPlace(row)}
          />
        ),
      },
    ];

    const isDisabled = this.state.enableActiveButton;
    const isDisabled1 = !this.state.enableActiveButton;
    const allActive = this.state.selectedRowData.every((row:any) => row.isActive);
    const allInactive = this.state.selectedRowData.every((row:any) => !row.isActive);
    const iconName = this.state.inActivePage 
    ? (allInactive ? 'ActivateOrders' : 'DeactivateOrders') 
    : (allActive ? 'DeactivateOrders' : 'ActivateOrders');

    const tooltipText = this.state.inActivePage 
    ? (allInactive ? `${this.props.t('BTN_ACTIVATE')}` : `${this.props.t('BTN_DEACTIVATE')}`)
    : '';
  
    return (
      <div className="layout width-100">
        <div className="bg-Color">
          <Grid
            lg={12}
            item
            container
            spacing={2}
            direction={'row'}
            style={{ marginRight: '-1.5px' }}
          >
            <Grid item lg={10.5} xs={10}>
              <Text
                className="color-blue text"
                key={'xxLarge'}
                variant={'xxLarge' as ITextProps['variant']}
                nowrap
                block
                styles={PIMHearderText}
              >
                {`${t('MENU_USERS')}`}
              </Text>
            </Grid>
            <Grid item lg={0.9} xs={0} />
            <Grid item lg={0.6} xs={1}>
              <Icon
                iconName="Download"
                className="iconStyle"
                title={this.props.t('BTN_DOWNLOAD')}
                onClick={() => {
                  if (this.state.currentRecord.length !== 0) {
                    this.downloadFile();
                  }
                }}
              />
            </Grid>
          </Grid>
          <Grid lg={12} item container spacing={1} direction={'row'}>
            <Grid item lg={2} xs={12}>
              <SearchBox
                className="searchBox"
                styles={PIMsearchBoxStyles}
                placeholder={t('SEARCH_PLACEHOLDER')}
                onChange={(e: any) => this.onSearch(e)}
                value={this.state.searchKey}
                onClear={() => this.setState({ searchKey: '' })}
              />
            </Grid>
            <Grid item lg={0.7} xs={6}>
              <Text className={activeText}>{t('ACTIVE')}</Text>
            </Grid>
            <Grid item lg={1.3} xs={6}>
              <Dropdown
                className="dropdown"
                placeholder="selectoption"
                onChange={this.handleDropdownChange}
                selectedKeys={this.state.divisionSelected}
                options={this.options}
                styles={PIMdropdownStyles}
                multiSelect
              />
            </Grid>

            <Grid item lg={1.3} xs={6}>
              <span title={this.props.t('ROLE_USER_BTN_ADD')}>
                <Icon
                  iconName="CircleAddition"
                  className="buttonIcon"
                  onClick={() => {
                    if (!isDisabled) {
                      this.setState({ visible: true });
                    }
                  }}
                  style={{
                    cursor: isDisabled ? 'not-allowed' : 'pointer',
                    color: isDisabled ? 'gray' : 'black',
                  }}
                />
              </span>
            </Grid>
            <Grid item lg={1.3} xs={6}>
              <span title={tooltipText}>
                <Icon
                  iconName={iconName}
                  className="buttonIcon1"
                  onClick={() => {
                    if (!isDisabled1) {
                      this.setState({
                        A_D_Visible: true,
                        // noSelection: !this.state.noSelection,
                        // enableEditButton: false,
                        // enableActiveButton: false,
                      });
                    }
                  }}
                  style={{
                    cursor: isDisabled1 ? 'not-allowed' : 'pointer',
                    color: isDisabled1 ? 'gray' : 'inherit',
                  }}
                />
              </span>
            </Grid>
            <Grid item lg={1.6} xs={6}>
              <span title={this.props.t('USER_BTN_ASSIGN_MARKETPLACE')}>
                <Icon
                  iconName="AddWork"
                  className="buttonIcon2"
                  onClick={() => {
                    if (
                      (this.state.inActivePage === 0 ||
                        this.state.inActivePage === 1 ||
                        this.state.inActivePage === 2) &&
                      !this.state.enableEditButton
                    ) {
                      return;
                    }
                    this.assignMarrketPlace();
                  }}
                  style={{
                    cursor:
                      (this.state.inActivePage === 0 ||
                        this.state.inActivePage === 1 ||
                        this.state.inActivePage === 2) &&
                      !this.state.enableEditButton
                        ? 'not-allowed'
                        : 'pointer',
                    color:
                      (this.state.inActivePage === 0 ||
                        this.state.inActivePage === 1 ||
                        this.state.inActivePage === 2) &&
                      !this.state.enableEditButton
                        ? 'gray'
                        : 'inherit',
                  }}
                />
              </span>

              {this.roleId.includes(1) && (
                <span title={this.props.t('USER_DELETED')}>
                  <Icon
                    iconName="Delete"
                    className="buttonIcon"
                    onClick={() => {
                      if (!isDisabled1) {
                        // this.DeleteUser();
                        this.setState({ A_D_Visible1: true });
                      }
                    }}
                    style={{
                      cursor: isDisabled1 ? 'not-allowed' : 'pointer',
                      color: isDisabled1 ? 'gray' : 'inherit',
                    }}
                  />
                </span>
              )}
            </Grid>

            <Grid item lg={3.3} />

            <Grid item lg={0.5} xs={2}>
              <Icon
                iconName="Refresh"
                title={this.props.t('BTN_REFRESH')}
                className="iconStyle"
                onClick={() => {
                  this.setState({ divisionSelected: '' }, () => {
                    this.refresh(2);
                  });
                }}             
              />
            </Grid>
          </Grid>
          <Modal
            isOpen={this.state.A_D_Visible}
            containerClassName={PIMcontentStyles.confirmContaineruser1}
            isBlocking={false}
            onDismiss={() =>
              this.setState({
                A_D_Visible: false,
              })
            }
          >
            <div className={PIMcontentStyles.confirmHeaderUser1}>
              <IconButton
                styles={UploadStyles}
                iconProps={cancelIcon}
                ariaLabel="Close popup modal"
                onClick={() => {
                  this.setState({
                    A_D_Visible: false,
                    noSelection: !this.state.noSelection,
                    enableEditButton: false,
                    enableActiveButton: false,
                    selectedRowData: [],
                    divisionSelected: ''
                  });
                  this.refresh(2);
                }}
              />
            </div>
            <div className={PIMcontentStyles.confirmbody4}>
              {this.state.inActivePage == false || allActive? (
                <Text variant="xLarge" className="apptext2 activatetxt">{`${t(
                  'CONFIRM_DEACTIVATE',
                )}`}</Text>
              ) : (
                <Text variant="xLarge" className="apptext2 activatetxt">{`${t('CONFIRM_ACTIVATE')}`}</Text>
              )}
            </div>

            <div className={PIMcontentStyles.footer}>
              <Grid lg={12} item container spacing={2}>
                <Grid item lg={1.5} xs={12} />
                <Grid item lg={4} xs={12}>
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonConfirm}
                    text={`${t('BTN_CONFIRM')}`}
                    onClick={this.onActiaveClick.bind(this)}
                  />
                </Grid>
                <Grid item lg={0.5} xs={12} />
                <Grid item lg={4} xs={12}>
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonCancel}
                    text={`${t('BTN_CANCEL')}`}
                    onClick={() => {
                      this.setState({
                        A_D_Visible: false,
                        noSelection: !this.state.noSelection,
                        enableEditButton: false,
                        enableActiveButton: false,
                        selectedRowData: [],
                        divisionSelected:''
                      });
                      this.refresh(2);
                    }}
                  />
                </Grid>
                <Grid item lg={2} xs={12} />
              </Grid>
            </div>
          </Modal>

          <Modal
            isOpen={this.state.A_D_Visible1}
            containerClassName={PIMcontentStyles.confirmContaineruser1}
            isBlocking={false}
            onDismiss={() =>
              this.setState({
                A_D_Visible1: false,
              })
            }
          >
            <div className={PIMcontentStyles.confirmHeaderUser1}>
              <IconButton
                styles={UploadStyles}
                iconProps={cancelIcon}
                ariaLabel="Close popup modal"
                onClick={() => {
                  this.setState({
                    A_D_Visible1: false,
                    noSelection: !this.state.noSelection,
                    enableEditButton: false,
                    enableActiveButton: false,
                    selectedRowData: [],
                    divisionSelected:''
                  });
                  this.refresh(2);
                }}
              />
            </div>
            <div className={PIMcontentStyles.confirmbody4}>
              <Text variant="xLarge" className="apptext2 dlttxt">{`${t('CONFIRM_DELETE')}`}</Text>
            </div>

            <div className={PIMcontentStyles.footer}>
              <Grid lg={12} item container spacing={2}>
                <Grid item lg={1.5} xs={12} />
                <Grid item lg={4} xs={12}>
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonConfirm}
                    text={`${t('BTN_CONFIRM')}`}
                    onClick={this.DeleteUser.bind(this)}
                  />
                </Grid>
                <Grid item lg={0.5} xs={12} />
                <Grid item lg={4} xs={12}>
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonCancel}
                    text={`${t('BTN_CANCEL')}`}
                    onClick={() => {
                      this.setState({
                        A_D_Visible1: false,
                        noSelection: !this.state.noSelection,
                        enableEditButton: false,
                        enableActiveButton: false,
                        selectedRowData: [],
                        divisionSelected:''
                      });
                      this.refresh(2);
                    }}
                  />
                </Grid>
                <Grid item lg={2} xs={12} />
              </Grid>
            </div>
          </Modal>
          <Modal
            isOpen={this.state.visible}
            containerClassName={PIMcontentStyles.attributeContainer}
            isBlocking={false}
            onDismiss={() => this.setState({ visible: false })}
          >
            <div className={PIMcontentStyles.header}>
              <div className="apptext1">
                {this.state.selectedRowData.length !== 0
                  ? `${this.props.t('EDIT_USER')}`
                  : `${this.props.t('ADD_USER')}`}
              </div>
              <div className="UserADDorUpdate">
                <IconButton
                  styles={iconButtonStyles}
                  iconProps={cancelIcon}
                  ariaLabel="Close popup modal"
                  onClick={() => {
                    this.setState({ visible: false, divisionSelected:'' });
                    this.refresh(2);
                  }
                }
                />
              </div>
            </div>
            <div className={PIMcontentStyles.body}>
              <AddorUpdateUser
                t={this.props.t}
                fields={this.state.selectedRowData}
                fields1={this.state.selectableRowskk}
                ClosePopup={() => {
                  this.setState({
                    visible: false,
                    noSelection: !this.state.noSelection,
                    enableEditButton: false,
                    enableActiveButton: false,
                    selectedRowData: [],
                    divisionSelected:''
                  })
                  this.refresh(2);
                }
                }
                Refresh={() => this.refresh(2)}
              />
            </div>
          </Modal>
          <Modal
            isOpen={this.state.rolevisible}
            containerClassName={PIMcontentStyles.tableContainer}
            isBlocking={false}
            onDismiss={() =>
              this.setState({
                rolevisible: false,
              })
            }
          >
            <div className={contentStyles.header}>
              <span className="apptext1">{`${
                this.state.isAssignDepartment ? t('MSG_ASSIGN_DIVISION') : t('MSG_ASSIGN_ROLE')
              } ${this.state.selectedRowData[0]?.name}`}</span>
            </div>
            <div className={PIMcontentStyles.body}>
              <form>
                <Stack
                  enableScopedSelectors
                  horizontal
                  disableShrink
                  styles={itemAlignmentsStackStyles}
                  tokens={itemAlignmentsStackTokens}
                >
                  <Grid>
                    <Dropdown
                      className="dropdown"
                      placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                      options={this.state.roleOptions}
                      onChange={(e: any, selectedOption: any) => this.onRoleChange(selectedOption)}
                      styles={AssigndropdownStyles}
                      multiSelect={!this.state.isAssignDepartment}
                      disabled={this.state.setDropdown}
                    />
                  </Grid>
                  <Grid>
                    <DefaultButton
                      className="button"
                      disabled={this.state.selectedRoleIds == 0}
                      iconProps={{ iconName: 'Add' }}
                      style={{ marginTop: '-1%' }}
                      onClick={this.addUserRole.bind(this)}
                    />
                  </Grid>
                </Stack>
              </form>
              <div className="dataTableFixedFooter">
                <DataTable
                  className="ms-Label"
                  columns={roleColumn}
                  data={this.state.roledata}
                  paginationPerPage={this.Defaultperpage()}
                  paginationRowsPerPageOptions={this.perpage()}
                  noDataComponent={
                    this.state.isAssignDepartment
                      ? t('NO_DIVISION_ASSIGNED')
                      : t('No_ROLES_ASSIGNED')
                  }
                />
              </div>
            </div>
          </Modal>
          <Modal
            isOpen={this.state.masterplaceVisible}
            containerClassName={PIMcontentStyles.tableContainer}
            isBlocking={false}
            onDismiss={() =>
              this.setState({
                masterplaceVisible: false,
              })
            }
          >
            <div className={contentStyles.header}>
              <span className="apptext1">{`${t('MSG_ASSIGN_MARKET_PLACE')} ${
                this.state.selectedRowData[0]?.name
              }`}</span>
              <IconButton
                styles={UserMarketplaceStyles}
                iconProps={cancelIcon}
                ariaLabel="Close popup modal"
                onClick={() => {
                  this.setState({
                    masterplaceVisible: false,
                    noSelection: !this.state.noSelection,
                    enableEditButton: false,
                    enableActiveButton: false,
                    selectedRowData: [],
                    divisionSelected: ''
                  });
                  this.refresh(2);
                }}
              />
            </div>
            <div className={PIMcontentStyles.body}>
              <form>
                <Stack
                  enableScopedSelectors
                  horizontal
                  disableShrink
                  styles={itemAlignmentsStackStyles}
                  tokens={itemAlignmentsStackTokens}
                >
                  <Grid>
                    <Dropdown
                      className="dropdown"
                      placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                      options={this.state.MarketAssignOption}
                      onChange={(e: any, selectedOption: any) =>
                        this.MarketOptionChange(selectedOption)
                      }
                      styles={AssigndropdownStyles}
                      multiSelect={true}
                    />
                  </Grid>
                  <Grid>
                    <DefaultButton
                      className="button"
                      disabled={this.state.selectedRoleIds == 0}
                      iconProps={{ iconName: 'Add' }}
                      style={{ marginTop: '-1%' }}
                      onClick={this.AddMarketPlaceToUser}
                    />
                  </Grid>
                </Stack>
              </form>
              <div className="dataTableFixedFooter">
                <DataTable
                  className="ms-Label"
                  columns={MarketPlaceColumn}
                  data={this.state.assignedMarketPlaceData}
                  paginationPerPage={this.Defaultperpage()}
                  paginationRowsPerPageOptions={this.perpage()}
                  noDataComponent={
                    this.state.isRecord == true
                      ? `${this.props.t('RECORDS')}`
                      : `${this.props.t('NO_RECORDS')}`
                  }
                />
              </div>
            </div>
          </Modal>
          <div className="dataTableFixedFooter">
            <DataTable
              columns={columns}
              data={currentRecord}
              pagination={true}
              selectableRows={this.state.selectedRowData}
              onSelectedRowsChange={this.rowSelection}
              clearSelectedRows={this.state.noSelection}
              paginationPerPage={this.Defaultperpage()}
              paginationRowsPerPageOptions={this.perpage()}
              selectableRowsHighlight
              highlightOnHover
              responsive
              fixedHeader
              striped
              fixedHeaderScrollHeight="69.1vh"
              style={{ marginBottom: '20px' }}
              paginationComponentOptions={{ rowsPerPageText: `${this.props.t('ROWS_PER_PAGE')}` }}
              noDataComponent={
                <div className="noDataWidth">
                  {<DataTable columns={columns} data={[{ '': '' }]} />}
                  <Stack className="noRecordsWidth">
                    {this.state.isRecord == true
                      ? `${this.props.t('RECORDS')}`
                      : `${this.props.t('NO_RECORDS')}`}
                  </Stack>
                </div>
              }
            />
          </div>
        </div>
      </div>
    );
  }
}

const ComponentTranslated: any = withTranslation()(User);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
