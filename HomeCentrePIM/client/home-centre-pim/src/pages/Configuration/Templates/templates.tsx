import {
  DefaultButton,
  Dropdown,
  Icon,
  IconButton,
  IDropdownOption,
  Modal,
  SearchBox,
  Stack,
} from '@fluentui/react';
import React, { Suspense } from 'react';
import DataTable from 'react-data-table-component';
import { Text } from '@fluentui/react/lib/Text';
import { Grid } from '@mui/material';
import {
  PIMButtons,
  PIMcontentStyles,
  PIMsearchBoxStyles,
  columnHeader,
  PIMdropdownStyles,
  activeText,
  PIMfooterButtonConfirm,
  PIMfooterButtonCancel,
  hyperlink,
  EditButtons,
  PIMHearderText,
} from '../../PimStyles';
import { Link } from 'react-router-dom';
import ApiManager from '@/services/ApiManager';
import AuthManagerService from '@/services/AuthManagerService';
import { withTranslation } from 'react-i18next';
import jwt_decode from 'jwt-decode';
import AddOrUpdateTemplate from './AddorUpdateTemplate';
import NotificationManager from '@/services/NotificationManager';
import {
  CancelButtonStyles,
  cancelIcon,
  CancelStyles,
  iconButtonStyles,
  UploadStyles,
  UploadStyles1,
} from '@/common/properties';
import axios from 'axios';
import { baseUrl } from '@/services/Constants';
import { IsActiveFilter } from '@/services/ApiClient';

class Templates extends React.Component<any, any> {
  authManager = new AuthManagerService();
  notify = new NotificationManager();
  apiClient = new ApiManager().CreateApiClient();
  token = sessionStorage.getItem('token');
  decode: any = jwt_decode(this.token || '');
  roleId: any = this.decode.RoleId.split(',').map(Number);
  roleLength = this.decode?.RoleId.split(',').length;
  options: IDropdownOption[] = [
    { key: 2, text: 'SelectAll' },
    { key: 0, text: 'Yes' },
    { key: 1, text: 'No' },
  ];

  constructor(props: any) {
    super(props);
    this.state = {
      templateData: [],
      searchData: [],
      selectionCount: 0,
      selectedRowData: [],
      noSelection: false,
      visible: false,
      isRecord: false,
      Template: false,
      searchValue: '',
      templateName: '',
      msgVisible: false,
      message: '',
      messageType: false,
      confirmModel: false,
      AssignTemplate: false,
      division: null,
      divisionOption: [],
      roleOption: [],
      roleSelected: [],
      role: [],
      userName: [],
      statusOption: [],
      divisionId: [],
      isActive: true,
      fields: [],
      A_D_Visible1: false,
      filtervalue: 1,
      divisionSelected: '',
      downloadExcel: '',
    };
  }

  componentDidMount(): void {
    this.refresh(2);
  }

  async refresh(isActive: number) {
    this.setState({ isRecord: true, isActive});
    // const key = '';
    // const pageNumber = 0;
    // const pageCount = 0;
    var user: any = [];
    var division: any = [];
    var role: any = [];
    var status: any = [];
    const divisions = await this.apiClient.getDivision();
    divisions.result?.forEach((element: any) => {
      division.push({ key: element.id, text: element.name });
    });
    const roles = await this.apiClient.getRoles(true);
    roles.result?.forEach((option: any) => {
      role.push({ key: option.id, text: option.name });
    });
    const users = (await this.apiClient.searchUsers(true)).result?.records;
    users?.forEach((option: any) => {
      user.push({ key: option.id, text: option.name });
    });
    const statuss = await this.apiClient.getTemplateStatus();
    statuss.result?.forEach((element: any) => {
      status.push({ key: element.id, text: element.name });
    });
    const response = await this.apiClient.searchTemplatesIsactive(isActive);
    this.setState({
      roleOption: role,
      divisionOption: division,
      userName: user,
      templateData: response.result,
      statusOption: status,
      noSelection: !this.state.noSelection,
      selectedRowData: [],
      selectionCount: 0,
      isRecord: false,
      divisionId: [],
    });
    this.setState({
      templateData: response.result?.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      })),
      searchValue: '',
      searchData: response.result?.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      })),
    });
  }

  async publishFile() {
    try {
      const selectedRecordIds = [];
      for (let i = 0; i < this.state.selectedRowData.length; i++) {
        selectedRecordIds.push(this.state.selectedRowData[i].id);
      }
      this.setState({ confirmPublish: false });
      const response = await this.apiClient.publishTemplate(selectedRecordIds);
      if (response.hasError) {
        this.notify.showErrorNotify(response.message || this.props.t('ERROR_UNKNOWN'));
      } else {
        this.notify.showSuccessNotify(this.props.t('PUBLISHED_SUCCESSFULLY'));
      }
      this.refresh(2);
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
    this.setState({divisionSelected:''})
    this.refresh(2);
  }
  perpage = () => {
    const recordCont = [25, 50, 100, 150, 200];
    return recordCont;
  };
  Defaultperpage = () => {
    const recordCont = 25;
    return recordCont;
  };
  rowSelection = (e: any) => {
    this.setState({ selectedRowData: e.selectedRows, selectionCount: e.selectedCount });
  };

  DeactivateTemplate = async () => {
    try {
      const selectedRecordIds = [];
      for (let i = 0; i < this.state.selectedRowData.length; i++) {
        selectedRecordIds.push(this.state.selectedRowData[i].id);
      }
      const allActive = this.state.selectedRowData.every((row:any) => !row.isActive);

      const response: any = this.state.isActive && allActive
      ? await this.apiClient.activateTemplates(selectedRecordIds)
      : await this.apiClient.deactivateTemplates(selectedRecordIds);
    let message =
      response.hasError == true
        ? response.message || `${this.props.t('ERROR_UNKNOWN')}`
        : this.state.isActive && allActive
        ? `${this.props.t('MSG_ACTIVATE')}`
        : `${this.props.t('MSG_DEACTIVATE')}`;
    this.setState({ confirmModel: false });
    this.notify.showSuccessNotify(message);
    this.refresh(2);
  } catch (e: any) {
    this.notify.showErrorNotify(e);
  }
  this.setState({divisionSelected:''})
  this.refresh(2);
}

  onSearch(e: any) {
    var newValue = e?.target.value;
    newValue = newValue == undefined ? '' : newValue;
    this.setState({ searchValue: newValue });
    var result = this.state.searchData.filter((element: any) => {
      return element?.name
        ?.toLowerCase()
        .includes(newValue.trim().split(/ +/).join(' ').toLowerCase());
    });
    this.setState({ templateData: result });
    this.setState({
      templateData: result?.map((item: any, index: any) => ({ index: index + 1, ...item })),
    });
  }

  async onEdit(row: any) {
    if (this.state.selectedRowData[0]) {
      try {
        this.setState({
          selectedRowData: [row],
        });

        const response = await this.apiClient.getTemplateRoles(this.state.selectedRowData[0].id);

        if (response.result) {
          const roleIds = response.result.map((option: any) => option.roleId);

          this.setState(
            {
              fields: {
                templateName: this.state.selectedRowData[0].name,
                roleIds: roleIds,
              },
            },
            () => {
              this.setState({
                Template: true,
                divisionId: this.state.selectedRowData[0].divisionId,
              });
            },
          );
        } else {
          console.error('Response result is undefined');
        }
      } catch (error) {
        console.error('Error fetching roleIds:', error);
      }
    }
  }

  roleOption = (event: React.FormEvent<HTMLDivElement>, item: IDropdownOption): void => {
    if (item) {
      this.setState({
        role: item.selected
          ? [...this.state.roleSelected, item.key as string]
          : this.state.roleSelected,
      });
    }
  };

  onMultiSelecting(event: any) {
    let data = this.state.divisionId;
    if (event.selected) {
      data.push({ key: event.key, text: event.text });
    } else {
      data = this.state.divisionId.filter((value: any) => {
        return value.key != event.key;
      });
    }
    this.setState({ divisionId: data });
  }
  onResponse = (r: any, message: any) => {
    this.setState({
      divisionId: 0,
      noSelection: !this.state.noSelection,
      selectedRowData: [],
      selectionCount: 0,
    });
    if (r.hasError) {
      this.notify.showErrorNotify(message);
    } else {
      this.notify.showSuccessNotify(message);
    }
    this.refresh(2);
  };

  downloadFile = () => {
    this.notify.showSuccessNotify1(this.props.t('DOWNLOAD_STARTED'));
    document.body.classList.add('loading-indicator');

    const { downloadExcel } = this.state;

    const _url =
      downloadExcel === true
        ? `${baseUrl}/api/Template/TemplatesExportExcel?isActive=true`
        : downloadExcel === false
        ? `${baseUrl}/api/Template/TemplatesExportExcel?isActive=false`
        : `${baseUrl}/api/Template/TemplatesExportExcel`;

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
        link.setAttribute('download', 'PIMTemplatesExcel.xlsx');
        link.click();

        window.URL.revokeObjectURL(url);
      })
      .catch((error: any) => {
        document.body.classList.remove('loading-indicator');
        console.error('Error:', error);
        this.notify.showErrorNotify(error);
      });
  };

  DeleteTemplates = async () => {
    this.setState({ A_D_Visible1: false });
    try {
      if (this.state.selectedRowData && this.state.selectedRowData.length > 0) {
        const ids = this.state.selectedRowData.map((e: any) => parseInt(e.id, 10));
        await this.apiClient.deleteTemplate(ids);
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
        // Handle "Select All"
        if (option.selected) {
          // Select all options including "Select All"
          selectedKeys = options.map((opt: IDropdownOption) => opt.key as string);
        } else {
          // Deselect all options including "Select All"
          selectedKeys = [];
        }
      } else {
        // Handle individual options
        if (option.selected) {
          // Add individual option key to selectedKeys
          if (!selectedKeys.includes(option.key as string)) {
            selectedKeys.push(option.key as string);
          }
        } else {
          // Remove individual option key from selectedKeys
          selectedKeys = selectedKeys.filter((key) => key !== (option.key as string));
        }

        // Toggle logic for 0 and 1
        if (selectedKeys.includes(0) && selectedKeys.includes(1)) {
          // If both 0 and 1 are selected, add "Select All" (2) to selectedKeys
          if (!selectedKeys.includes(2)) {
            selectedKeys.push(2);
          }
        } else if (!selectedKeys.includes(0) || !selectedKeys.includes(1)) {
          // If either 0 or 1 is deselected, remove "Select All" (2) from selectedKeys
          selectedKeys = selectedKeys.filter((key) => key !== 2);
        }
      }

      // Ensure "Select All" reflects the correct state
      const allOptionsSelected = options.length > 0 && selectedKeys.length === options.length;
      if (allOptionsSelected) {
        // Ensure "Select All" is included if all options are selected
        if (!selectedKeys.includes(2)) {
          selectedKeys = [2, ...selectedKeys];
        }
      } else {
        // Remove "Select All" if not all options are selected
        selectedKeys = selectedKeys.filter((key) => key !== 2);
      }

      this.setState(
        {
          divisionSelected: selectedKeys,
          inActivePage: this.state.inActivePage, // Ensure to use current state value if needed
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
            let statusname;
            if (inActivePage === 2) {
              statusname = null;
            } else if (inActivePage === 1) {
              statusname = false;
            } else if (inActivePage === 0) {
              statusname = true;
            } else {
              statusname = null;
            }
            this.setState({ downloadExcel: statusname }, () => {});
          });
        },
      );
    }
  };
  render() {
    const { t } = this.props;
    const columns: any = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{`${this.props.t('S_NO')}`}</div>,
        selector: (row: any) => row.index,
        isVisible: true,
        width: '10%',
      },
      {
        key: 'actionColumn',
        name: <div className={columnHeader}>{`${this.props.t('COL_ACTION')}`}</div>,
        cell: (row: any) =>
          this.state.templateData != 0 && (
            <div>
              <span
                className={hyperlink}
                onClick={() => {
                  this.setState({ selectedRowData: [row] }, () => {
                    this.onEdit(row);
                  });
                }}
                role="button"
                tabIndex={0}
                aria-label="Edit"
              >
                <Icon styles={EditButtons} iconName="Edit" />
              </span>
            </div>
          ),
        isVisible: true,
        width: '9%',
      },
      {
        key: 'templates',
        name: <div className={columnHeader}>{`${this.props.t('TEMPLATES')}`}</div>,
        selector: (row: any) => {
          var value = [row.id, row.divisionId, row.name];
          return (
            <div>
              <Link to={{ pathname: `/configuration/template/addattribute/${value}` }}>
                {' '}
                {row.name}
              </Link>
            </div>
          );
        },
        isVisible: true,
        minwidth: '500px',
      },
      {
        key: 'divisionColumn',
        name: <div className={columnHeader}>{`${this.props.t('DIVISION')}`}</div>,
        selector: (row: any) => {
          const divisionText = this.state.divisionOption.find(
            (e: any) => e.key == row.divisionId,
          )?.text;
          return <span title={divisionText}>{divisionText}</span>;
        },
        isVisible: true,
        minwidth: '500px',
      },
      {
        key: 'dateCreated',
        name: <div className={columnHeader}>{`${this.props.t('DATE_CREATED')}`}</div>,
        selector: (row: any) => {
          const formattedDate =
            row.dateCreated != null && row.dateCreated != ''
              ? new Date(row.dateCreated).toLocaleDateString('en-GB')
              : '';
          return <span title={formattedDate}>{formattedDate}</span>;
        },
        isVisible: true,
      },
      {
        key: 'createdBy',
        name: <div className={columnHeader}>{`${this.props.t('CREATED_BY')}`}</div>,
        selector: (row: any) => {
          const userNameText = this.state.userName.find((e: any) => e.key == row.createdBy)?.text;
          return <span title={userNameText}>{userNameText}</span>;
        },
        isVisible: true,
      },
      {
        key: 'status',
        name: <div className={columnHeader}>{`${this.props.t('STATUS')}`}</div>,
        selector: (row: any) => {
          const statusText = this.state.statusOption.find(
            (e: any) => e.key == row.templateStatusId,
          )?.text;
          return <span title={statusText}>{statusText}</span>;
        },
        isVisible: true,
      },
    ];
    const handleColumnReorder = (newColumns: any) => {
      var column: any = [...columns];
      if (newColumns == this.state.currentColumns) {
        this.setState({ currentColumns: column });
      } else {
        this.setState({ currentColumns: newColumns });
      }
    };

    const allActive = this.state.selectedRowData.every((row:any) => row.isActive);
    const allInactive = this.state.selectedRowData.every((row:any) => !row.isActive);

    return (
      <div className="layout width-100">
        <div className="bg-Color">
          <Grid lg={12} item container spacing={2} direction={'row'}>
            <Grid item lg={10.5} xs={10}>
              {/* <Text block className="text">{`${t('PIM')} / ${t('TEMPLATES')}`}</Text> */}
              <Text variant="xxLarge" styles={PIMHearderText} className="color-blue text">{`${t(
                'LIST_OF_TEMPLATES',
              )}`}</Text>
            </Grid>
            <Grid item lg={0.9} xs={0} />
            <Grid item lg={0.6} xs={1}>
              <Icon
                iconName="Download"
                className="iconStyle"
                title={this.props.t('BTN_DOWNLOAD')}
                onClick={() => {
                  if (this.state.templateData.length !== 0) {
                    this.downloadFile();
                  }
                }}
              />
            </Grid>
          </Grid>
          <Grid lg={12} item container spacing={2} direction={'row'}>
            <Grid item lg={2} xs={12}>
              <SearchBox
                className="searchBox"
                styles={PIMsearchBoxStyles}
                placeholder={t('TEMPLATE_SEARCH')}
                onChange={this.onSearch.bind(this)}
                value={this.state.searchValue}
                onClear={() => this.setState({ searchValue: '' })}
              />
            </Grid>
            <Grid item lg={0.7} xs={6}>
              <Text className={activeText}>{t('ACTIVE')}</Text>
            </Grid>
            <Grid item lg={1} xs={6}>
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
              <span title={t('ROLE_ADD_BTN')}>
                <Icon
                  iconName="CircleAddition"
                  className="buttonIcon"
                  onClick={() => {
                    if ((this.state.isActive === 0 || this.state.isActive === 1 || this.state.isActive ===2) && this.state.selectionCount < 1) {
                      this.setState({
                        Template: true,
                        selectedOption: [],
                        selectedRowData: [],
                        fields: { roleIds: [] },
                      });
                    }
                  }}
                  style={{
                    cursor:
                    (this.state.isActive === 0 || this.state.isActive === 1 || this.state.isActive ===2) && this.state.selectionCount < 1
                        ? 'pointer'
                        : 'not-allowed',
                    color:
                    (this.state.isActive === 0 || this.state.isActive === 1 || this.state.isActive ===2) && this.state.selectionCount < 1 ? 'inherit' : 'gray',
                  }}
                />
              </span>
            </Grid>

            {/* {this.state.isActive && (
              <Grid item lg={0.9} xs={12}>
                {this.roleId.indexOf('1') > -1 && (
                  <DefaultButton
                    styles={PIMButtons}
                    iconProps={{ iconName: 'Edit' }}
                    text={`${t('BTN_EDIT')}`}
                    className="button"
                    disabled={this.state.selectionCount == 1 ? false : true}
                    onClick={() => this.onEdit()}
                  />
                )}
              </Grid>
            )} */}
            <Grid item lg={1.2} xs={6}>
              {this.state.isActive === 0 ? (
               <span title={allInactive ? this.props.t('BTN_ACTIVATE') : this.props.t('BTN_DEACTIVE')}>
                  <Icon
                    iconName={allInactive ? 'ActivateOrders' : 'DeactivateOrders'}
                    className="buttonIcon1"
                    onClick={() => {
                      if (this.state.selectionCount > 0) {
                        this.setState({ confirmModel: true });
                      }
                    }}
                    style={{
                      cursor: this.state.selectionCount > 0 ? 'pointer' : 'not-allowed',
                      color: this.state.selectionCount > 0 ? 'inherit' : 'gray',
                    }}
                  />
                </span>
              ) : (
                <span title={allInactive ? this.props.t('BTN_ACTIVATE') : this.props.t('BTN_DEACTIVE')}>
                  <Icon
                    iconName={allActive ? 'DeactivateOrders' : 'ActivateOrders'}
                    className="buttonIcon1"
                    onClick={() => {
                      if (this.state.selectionCount > 0) {
                        this.setState({ confirmModel: true });
                      }
                    }}
                    style={{
                      cursor: this.state.selectionCount > 0 ? 'pointer' : 'not-allowed',
                      color: this.state.selectionCount > 0 ? 'inherit' : 'gray',
                    }}
                  />
                </span>
              ) }
            </Grid>
            <Grid>
              {this.roleId.includes(1) ? (
                <span title={this.props.t('DELETE_TEMPLATE')}>
                  <Icon
                    iconName="Delete"
                    className="buttonIcon8"
                    onClick={() => {
                      if (this.state.selectionCount > 0) {
                        // this.DeleteTemplates();
                        this.setState({ A_D_Visible1: true });
                      }
                    }}
                    style={{
                      cursor: this.state.selectionCount > 0 ? 'pointer' : 'not-allowed',
                      color: this.state.selectionCount > 0 ? 'inherit' : 'gray',
                    }}
                  />
                </span>
              ) : null}
            </Grid>

            {this.state.isActive ? (
              <Grid item lg={1} xs={6}>
                <span title={t('BTN_PUBLISH')}>
                  <Icon
                    iconName="WebPublish"
                    className="buttonIcon3"
                    onClick={() => {
                      if (this.state.selectionCount > 0) {
                        this.setState({ confirmPublish: true });
                      }
                    }}
                    style={{
                      cursor: this.state.selectionCount > 0 ? 'pointer' : 'not-allowed',
                      color: this.state.selectionCount > 0 ? 'inherit' : 'gray',
                    }}
                  />
                </span>
              </Grid>
            ) : (
              <Grid item lg={1} xs={6}>
                <span title={t('BTN_PUBLISH')}>
                  <Icon
                    iconName="WebPublish"
                    className="buttonIcon3"
                    onClick={() => {
                      if (this.state.selectionCount > 0) {
                        this.setState({ confirmPublish: true });
                      }
                    }}
                    style={{
                      cursor: this.state.selectionCount > 0 ? 'pointer' : 'not-allowed',
                      color: this.state.selectionCount > 0 ? 'inherit' : 'gray',
                    }}
                  />
                </span>
              </Grid>
            )}
            <Grid item lg={this.state.isActive ? 4.3 : 4.3} xs={0} style={{ marginLeft: '-9px' }} />
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
            isOpen={this.state.confirmModel}
            containerClassName={PIMcontentStyles.confirmContaineruser1}
            onDismiss={() =>
              this.setState({
                confirmModel: false,
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
                    confirmModel: false,
                    noSelection: !this.state.noSelection,
                    selectedRowData: [],
                    selectionCount: 0,
                    divisionSelected:''
                  });
                  this.refresh(2);
                }}
              />
            </div>
            <div className={PIMcontentStyles.confirmbody4}>
              {this.state.isActive === 0 || allActive? (
                <Text variant="large" className="apptext2 activatetxt2">{`${t(
                  'CONFIRM_DEACTIVATE',
                )}`}</Text>
              ) : (
                <Text variant="large" className="apptext2 activatetxt1">{`${t(
                  'CONFIRM_ACTIVATE',
                )}`}</Text>
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
                    onClick={this.DeactivateTemplate.bind(this)}
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
                        confirmModel: false,
                        noSelection: !this.state.noSelection,
                        selectedRowData: [],
                        selectionCount: 0,
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
            isOpen={this.state.Template}
            containerClassName={PIMcontentStyles.container5}
            onDismiss={() =>
              this.setState({
                Template: false,
              })
            }
          >
            <div className={PIMcontentStyles.confirmHeaderUser}>
              <IconButton
                styles={UploadStyles1}
                iconProps={cancelIcon}
                ariaLabel="Close popup modal"
                onClick={() => {
                  this.setState({ 
                    Template: false, 
                    divisionSelected:'',
                  });
                  this.refresh(2);
                }
              }
              />
            </div>
            {this.state.selectedRowData.length == 0 ? (
              <Grid item xs={10}>
                <AddOrUpdateTemplate
                  CloseModal={() => {
                    this.setState({
                      Template: false,
                       divisionSelected:'',
                    })
                    this.refresh(2);
                  }
                  }
                  DivisionOption={this.state.divisionOption}
                  roleOption={this.state.roleOption}
                  selectedOption={this.state.selectedOption}
                  fields={this.state.selectedRowData}
                  selectedRole={this.state.fields.roleIds}
                  response={this.onResponse}
                />
              </Grid>
            ) : (
              <Grid item xs={10}>
                <AddOrUpdateTemplate
                  CloseModal={() =>
                    this.setState({
                      Template: false,
                       divisionSelected:''
                    })
                  }
                  DivisionOption={this.state.divisionOption}
                  roleOption={this.state.roleOption}
                  selectedOption={this.state.selectedOption}
                  fields={this.state.selectedRowData}
                  selectedRole={this.state.fields.roleIds}
                  response={this.onResponse}
                />
              </Grid>
            )}
          </Modal>
          <Modal
            isOpen={this.state.confirmPublish}
            containerClassName={PIMcontentStyles.confirmContaineruser1}
            onDismiss={() =>
              this.setState({
                confirmPublish: false,
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
                    confirmPublish: false,
                    noSelection: !this.state.noSelection,
                    selectedRowData: [],
                    selectionCount: 0,
                    divisionSelected:''
                  });
                  this.refresh(2);
                }}
              />
            </div>
            <div className={PIMcontentStyles.confirmbody4}>
              <Text variant="xLarge" className="apptext2 dlttxt">{`${t('CONFIRM_PUBLISH')}`}</Text>
            </div>
            <div className={PIMcontentStyles.footer}>
              <Grid lg={12} item container spacing={2}>
                <Grid item lg={1.5} xs={12} />
                <Grid item lg={4} xs={12}>
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonConfirm}
                    text={`${t('BTN_CONFIRM')}`}
                    onClick={this.publishFile.bind(this)}
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
                        confirmPublish: false,
                        noSelection: !this.state.noSelection,
                        selectedRowData: [],
                        selectionCount: 0,
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
                    selectedRowData: [],
                    selectionCount: 0,
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
                    onClick={this.DeleteTemplates.bind(this)}
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
                        selectedRowData: [],
                        selectionCount: 0,
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
          <div className="dataTableFixedFooter">
            <DataTable
              columns={columns}
              data={this.state.templateData}
              onColumnOrderChange={handleColumnReorder}
              pagination={true}
              selectableRows={this.state.selectedRowData}
              onSelectedRowsChange={this.rowSelection}
              clearSelectedRows={this.state.noSelection}
              selectableRowsHighlight
              highlightOnHover
              responsive
              fixedHeader
              striped
              fixedHeaderScrollHeight="69.1vh"
              paginationComponentOptions={{ rowsPerPageText: `${this.props.t('ROWS_PER_PAGE')}` }}
              paginationPerPage={this.Defaultperpage()}
              paginationRowsPerPageOptions={this.perpage()}
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

const ComponentTranslated = withTranslation()(Templates);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
