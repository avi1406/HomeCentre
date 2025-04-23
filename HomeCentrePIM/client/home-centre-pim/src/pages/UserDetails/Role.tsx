import React, { Suspense } from 'react';
import 'react-phone-input-2/lib/style.css';
import { SearchBox } from '@fluentui/react/lib/SearchBox';
import DataTable from 'react-data-table-component';
import { DefaultButton, IconButton } from '@fluentui/react/lib/Button';
import { Dropdown, IDropdownOption } from 'office-ui-fabric-react/lib/Dropdown';
import { ITextProps, Text } from '@fluentui/react/lib/Text';
import { Icon, Link, Modal, Stack } from '@fluentui/react';
import ApiManager from '@/services/ApiManager';
import 'office-ui-fabric-react/dist/css/fabric.css';
import {
  CancelButtonStyles,
  CancelUserStyles,
  CancelroleStyles,
  UploadStyles,
  cancelIcon,
  iconButtonStyles,
} from '@/common/properties';
import { withTranslation } from 'react-i18next';
import { Grid } from '@mui/material';
import NotificationManager from '@/services/NotificationManager';
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
import AddorUpdateRole from './AddorUpdateRole';
import axios from 'axios';
import { baseUrl } from '@/services/Constants';
import AuthManagerService from '@/services/AuthManagerService';
import jwt_decode from 'jwt-decode';

class Role extends React.Component<any, any> {
  authManager = new AuthManagerService();
  apiClient = new ApiManager().CreateApiClient();
  token = sessionStorage.getItem('token');
  decode: any = jwt_decode(this.token || '');
  roleId: any = this.decode.RoleId.split(',').map(Number);
  stausofRecords = true;
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
      filtervalue: 1,
      rolevisible: false,
      copyRecord: [],
      searchKey: '',
      selectedRoleIds: [],
      selectedRowData: [],
      noSelection: false,
      isRecord: true,
      divisionSelected: '',
    };
  }

  async componentDidMount(): Promise<void> {
    this.refresh(2);
  }
  async refresh(inActivePage: number) {
    try {
      this.setState({ searchKey: '', isRecord: true, inActivePage: inActivePage });
      const apiResponse: any = (await this.apiClient.searchAllRoles(inActivePage)).result?.records;
      this.setState({
        currentRecord: apiResponse?.map((item: any, index: any) => ({ index: index + 1, ...item })),
        copyRecord: apiResponse?.map((item: any, index: any) => ({ index: index + 1, ...item })),
      });
    } catch (e: any) {
      this.notify.showErrorNotify(e.message);
    }
    this.setState({
      A_D_Visible: false,
      noSelection: !this.state.noSelection,
      enableEditButton: false,
      enableActiveButton: false,
      selectedRowData: [],
      isRecord: false,
    });
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

  async onActiaveClick() {
    try {
      const selectedRecordIds = [];
      for (let i = 0; i < this.state.selectedRowData.length; i++) {
        selectedRecordIds.push(this.state.selectedRowData[i].id);
      }
      const allActive = this.state.selectedRowData.every((row:any) => !row.isActive);

      const response = this.state.inActivePage && allActive
        ? await this.apiClient.activateRoleBatch(selectedRecordIds)
        : await this.apiClient.deactivateRoleBatch(selectedRecordIds);
      if (response.hasError) {
        this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN'));
      } else {
        this.notify.showSuccessNotify(
          this.state.inActivePage && allActive
            ? `${this.props.t('MSG_ACTIVATE')}`
            : `${this.props.t('MSG_DEACTIVATE')}`,
        );
      }
      this.refresh(2);
    } catch (e: any) {
      this.notify.showErrorNotify(e.message);
    }
    this.setState({divisionSelected:''})
    this.refresh(2);
  }

  handleRowClick = (row: any) => {
    this.setState({
      visible: true,
      selectedRowData: [row],
    });
  };
  onSearch(e: any) {
    var newValue = e?.target.value;
    newValue = newValue == undefined ? '' : newValue;
    this.setState({ searchKey: newValue });
    var { copyRecord } = this.state;
    var result = copyRecord.filter((element: any) => {
      return element?.name
        ?.toLowerCase()
        .includes(newValue.trim().split(/ +/).join(' ').toLowerCase());
    });
    this.setState({
      currentRecord: result?.map((item: any, index: any) => ({ index: index + 1, ...item })),
    });
  }

  downloadFile = () => {
    this.notify.showSuccessNotify1(this.props.t('DOWNLOAD_STARTED'));
    document.body.classList.add('loading-indicator');

    const { inActivePage } = this.state;

    const _url =
      inActivePage === 0
        ? `${baseUrl}/api/User/RoleExportExcel?isActive=true`
        : inActivePage == 1
        ? `${baseUrl}/api/User/RoleExportExcel?isActive=false`
        : `${baseUrl}/api/User/RoleExportExcel`;

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
        link.setAttribute('download', 'RolesExcel.xlsx');
        link.click();

        window.URL.revokeObjectURL(url);
      })
      .catch((error: any) => {
        document.body.classList.remove('loading-indicator');
        console.error('Error:', error);
        this.notify.showErrorNotify(error);
      });
  };
  DeleteRoles = async () => {
    this.setState({ A_D_Visible1: false });
    try {
      if (this.state.selectedRowData && this.state.selectedRowData.length > 0) {
        const ids = this.state.selectedRowData.map((e: any) => parseInt(e.id, 10));
        await this.apiClient.deleteRole(ids);
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
        width: '15%',
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
        minwidth: '100px',
      },
      {
        key: 'descriptionColumn',
        name: <div className={columnHeader}>{t('TABLE_DESCRIPTION_DESCRIPTION')}</div>,
        selector: (row: any) => <span title={row.description}>{row.description}</span>,
        minwidth: '200px',
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
    const allActive = this.state.selectedRowData.every((row:any) => row.isActive);
    const allInactive = this.state.selectedRowData.every((row:any) => !row.isActive);
    return (
      <div className="layout width-100">
        <div className="bg-Color">
          <Grid lg={12} item container spacing={2} direction={'row'}>
            <Grid item lg={10.5} xs={10}>
              <Text
                className="color-blue text"
                key={'xxLarge'}
                variant={'xxLarge' as ITextProps['variant']}
                styles={PIMHearderText}
                nowrap
                block
              >
                {t('MENU_ROLES')}
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
            <Grid item lg={0.9} xs={6}>
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
                    if (
                      (this.state.inActivePage === 0 ||
                        this.state.inActivePage === 1 ||
                        this.state.inActivePage === 2) &&
                      !this.state.enableActiveButton
                    ) {
                      this.setState({ visible: true });
                    }
                  }}
                  style={{
                    cursor:
                      (this.state.inActivePage === 0 ||
                        this.state.inActivePage === 1 ||
                        this.state.inActivePage === 2) &&
                      !this.state.enableActiveButton
                        ? 'pointer'
                        : 'not-allowed',
                    color:
                      (this.state.inActivePage === 0 ||
                        this.state.inActivePage === 1 ||
                        this.state.inActivePage === 2) &&
                      !this.state.enableActiveButton
                        ? 'inherit'
                        : 'gray',
                  }}
                />
              </span>
            </Grid>

            {/* <Grid item lg={1.3} xs={6}>
              <DefaultButton
                className="button"
                type="primary"
                iconProps={{ iconName: 'EditSolid12' }}
                text={`${t('ROLE_USER_BTN_EDIT')}`}
                styles={PIMButtons}
                disabled={!this.state.enableEditButton || this.state.inActivePage}
                onClick={() => this.setState({ visible: true })}
              />
            </Grid> */}
            <Grid item lg={1.3} xs={6}>
            <span
    title={this.state.inActivePage 
      ? (allInactive ? t('BTN_ACTIVATE') : t('BTN_DEACTIVATE')) 
      : (allActive ? t('BTN_DEACTIVATE') : t('BTN_ACTIVATE'))
    }
  >

                <Icon
                  iconName={this.state.inActivePage 
                    ? (allInactive ? 'ActivateOrders' : 'DeactivateOrders') 
                    : (allActive ? 'DeactivateOrders' : 'ActivateOrders')}
                  className="buttonIcon1"
                  onClick={() => {
                    if (!this.state.enableActiveButton) {
                      return;
                    }
                    this.setState({
                      A_D_Visible: true,
                      // noSelection: !this.state.noSelection,
                      // enableEditButton: false,
                      // enableActiveButton: false,
                    });
                  }}
                  style={{
                    cursor: this.state.enableActiveButton ? 'pointer' : 'not-allowed',
                    color: this.state.enableActiveButton ? 'inherit' : 'gray',
                  }}
                />
              </span>
            </Grid>
            <Grid>
              {this.roleId.includes(1) && (
                <span title={this.props.t('DELETE_ROLE')}>
                  <Icon
                    iconName="Delete"
                    className="buttonIcon6"
                    onClick={() => {
                      if (!this.state.enableActiveButton) {
                        return;
                      }

                      this.setState({ A_D_Visible1: true });
                    }}
                    style={{
                      cursor: this.state.enableActiveButton ? 'pointer' : 'not-allowed',
                      color: this.state.enableActiveButton ? 'inherit' : 'gray',
                    }}
                  />
                </span>
              )}
            </Grid>

            <Grid item lg={4.7} />
            <Grid item lg={0.5} xs={1}>
              {/* <DefaultButton
                className="button"
                text={t('BTN_REFRESH')}
                iconProps={{ iconName: 'Refresh' }}
                styles={PIMButtons}
                onClick={() => this.refresh(!this.state.inActivePage)}
              /> */}
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
                    divisionSelected:''
                  });
                  this.refresh(2);
                }}
              />
            </div>
            <div className={PIMcontentStyles.confirmbody4}>
              {this.state.inActivePage == false || allActive? (
                <Text variant="xLarge" className="apptext2 activatetxt">{`${t('CONFIRM_DEACTIVATE')}`}</Text>
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
            isOpen={this.state.visible}
            containerClassName={PIMcontentStyles.container5}
            isBlocking={false}
            onDismiss={() =>
              this.setState({
                visible: false,
              })
            }
          >
            <div className={PIMcontentStyles.header}>
              <Grid container spacing={2}>
                <Grid item xs={10.5}>
                  <p>
                    {this.state.selectedRowData.length != 0 ? (
                      <span className="apptext1">{`${t('EDIT_ROLE')}`}</span>
                    ) : (
                      <span className="apptext1">{`${t('ADD_ROLE')}`}</span>
                    )}
                  </p>
                </Grid>
                <Grid item xs={1.5}>
                  <IconButton
                    styles={iconButtonStyles}
                    iconProps={cancelIcon}
                    ariaLabel="Close popup modal"
                    onClick={() => {
                      this.setState({
                        visible: false,
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
              </Grid>
            </div>
            <div className={PIMcontentStyles.body}>
              <AddorUpdateRole
                t={this.props.t}
                fields={this.state.selectedRowData}
                ClosePopup={() => {
                  this.setState({
                    visible: false,
                    noSelection: !this.state.noSelection,
                    enableEditButton: false,
                    enableActiveButton: false,
                    selectedRowData: [],
                    divisionSelected:''
                  });
                  this.refresh(2);
                }}
                Refresh={() => this.refresh(2)}
              />
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
                    onClick={this.DeleteRoles.bind(this)}
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
          <div className="dataTableFixedFooter">
            <DataTable
              columns={columns}
              data={currentRecord}
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

const ComponentTranslated: any = withTranslation()(Role);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
