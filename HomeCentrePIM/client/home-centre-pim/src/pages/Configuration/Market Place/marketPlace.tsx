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
  PIMHearderText,
} from '../../PimStyles';
import { Link } from 'react-router-dom';
import ApiManager from '@/services/ApiManager';
import AuthManagerService from '@/services/AuthManagerService';
import { withTranslation } from 'react-i18next';
import jwt_decode from 'jwt-decode';
import NotificationManager from '@/services/NotificationManager';
import AddOrUpdateMarketPlace from './AddOrUpdateMarketPlace';
import {
  CancelButtonStyles,
  cancelIcon,
  CancelStyles,
  iconButtonStyles,
  MarketplaceStyles,
  UploadStyles,
} from '@/common/properties';
import axios from 'axios';
import { baseUrl } from '@/services/Constants';

class MarketPlace extends React.Component<any, any> {
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
      marketData: [],
      selectionCount: 0,
      selectedRowData: [],
      noSelection: false,
      isRecord: false,
      isActive: false,
      marketPlace: false,
      searchData: [],
      searchValue: '',
      A_D_Visible: false,
      filtervalue: 1,
      enableActiveButton: false,
      divisionSelected: '',
      downloadExcel: '',
    };
  }

  componentDidMount(): void {
    this.refresh(2);
  }

  async refresh(isActive: number) {
    this.setState({ isRecord: true, isActive });
    const response = await this.apiClient.searchAllMarketPlace(isActive);
    this.setState({
      marketData: response.result?.records,
      noSelection: !this.state.noSelection,
      searchValue: '',
      selectedRowData: [],
      selectionCount: 0,
      isRecord: false,
    });
    this.setState({
      marketData: response.result?.records?.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      })),
      searchData: response.result?.records?.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      })),
    });
  }

  rowSelection = (e: any) => {
    this.setState({ selectedRowData: e.selectedRows, selectionCount: e.selectedCount });
  };
  perpage = () => {
    const recordCont = [25, 50, 100, 150, 200];
    return recordCont;
  };
  Defaultperpage = () => {
    const recordCont = 25;
    return recordCont;
  };
  onSearch(e: any) {
    var newValue = e?.target.value;
    newValue = newValue == undefined ? '' : newValue;
    this.setState({ searchValue: newValue });
    var result = this.state.searchData.filter((element: any) => {
      return element?.name
        ?.toLowerCase()
        .includes(newValue.trim().split(/ +/).join(' ').toLowerCase());
    });
    this.setState({ marketData: result });
    this.setState({
      marketData: result?.map((item: any, index: any) => ({ index: index + 1, ...item })),
    });
  }

  downloadFile = () => {
    this.notify.showSuccessNotify1(this.props.t('DOWNLOAD_STARTED'));
    document.body.classList.add('loading-indicator');

    const { downloadExcel } = this.state;

    // Build the base URL
    let url = `${baseUrl}/api/Marketplace/MarketPlaceExportExcel?isactive=`;

    // Add isactive parameter based on downloadExcel state
    if (downloadExcel === true) {
      url += 'true';
    } else if (downloadExcel === false) {
      url += 'false';
    }
    axios({
      method: 'POST',
      url: url,
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
        link.setAttribute('download', 'MarketPlaceExcel.xlsx');
        link.click();

        window.URL.revokeObjectURL(url);
      })
      .catch((error: any) => {
        document.body.classList.remove('loading-indicator');
        console.error('Error:', error);
        this.notify.showErrorNotify(error);
      });
  };

  async ActivateMarketPlace() {
    try {
      const selectedRecordIds = [];
      for (let i = 0; i < this.state.selectedRowData.length; i++) {
        selectedRecordIds.push(this.state.selectedRowData[i].id);
      }
      const allActive = this.state.selectedRowData.every((row:any) => !row.isActive);

      const response: any = this.state.isActive && allActive
      ? await this.apiClient.activateMarketplaces(selectedRecordIds)
      : await this.apiClient.deactivateMarketplaces(selectedRecordIds);

      // const allActive = this.state.selectedRowData.every((row:any) => row.isActive);

      // const response = !this.state.isActive && allActive
      //   ? await this.apiClient.deactivateMarketplaces(selectedRecordIds)
      //   : await this.apiClient.activateMarketplaces(selectedRecordIds);
      let message =
        response.hasError == true
          ? `${this.props.t('ERROR_UNKNOWN')}`
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

  onResponse = (r: any, message: any) => {
    this.refresh(2);
  };

  DeleteMarketPlace = async () => {
    this.setState({ A_D_Visible1: false });
    try {
      if (this.state.selectedRowData && this.state.selectedRowData.length > 0) {
        const ids = this.state.selectedRowData.map((e: any) => parseInt(e.id, 10));
        await this.apiClient.deleteMarketPlace(ids);
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
          isActive: this.state.isActive,
          currentPage: 1,
          noSelection: true,
          filtervalue: this.state.isActive === 1 ? 0 : 1,
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
        name: <div className={columnHeader}>{`${t('S_NO')}`}</div>,
        selector: (row: any) => row.index,
        isVisible: true,
        maxWidth: '200px',
      },
      {
        key: 'marketPlace',
        name: <div className={columnHeader}>{`${t('MARKET_PLACE')}`}</div>,
        selector: (row: any) => {
          const marketPlaceText = row.name;
          return <span title={marketPlaceText}>{marketPlaceText}</span>;
        },
        isVisible: true,
        minwidth: '500px',
      },
      {
        key: 'dateCreated',
        name: <div className={columnHeader}>{`${t('DATE_CREATED')}`}</div>,
        selector: (row: any) => {
          const formattedDate =
            row.dateCreated != null && row.dateCreated != ''
              ? new Date(row.dateCreated).toLocaleDateString('en-GB')
              : '';
          return <span title={formattedDate}>{formattedDate}</span>;
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
              {/* <Text block className="text">{`${t('PIM')} / ${t('MARKET_PLACE')}`}</Text> */}
              <Text variant="xxLarge" styles={PIMHearderText} className="color-blue text">{`${t(
                'MARKET_PLACE',
              )}`}</Text>
            </Grid>
            <Grid item lg={0.9} xs={0} />
            <Grid item lg={0.6} xs={1}>
              <Icon
                iconName="Download"
                className="iconStyle"
                title={this.props.t('BTN_DOWNLOAD')}
                onClick={() => {
                  if (this.state.marketData.length !== 0) {
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
                placeholder={t('MARKETPLACE_SEARCH')}
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
              <Grid item lg={1.3} xs={6}>
                <span title={t('ROLE_ADD_BTN')}>
                  <Icon
                    iconName="CircleAddition"
                    className="buttonIcon"
                    onClick={() => {
                      if (this.state.selectionCount < 1) {
                        this.setState({ marketPlace: true });
                      }
                    }}
                    style={{
                      cursor: this.state.selectionCount < 1 ? 'pointer' : 'not-allowed',
                      color: this.state.selectionCount < 1 ? 'inherit' : 'gray',
                    }}
                  />
                </span>
              </Grid>
            </Grid>

            <Grid item lg={1.2} xs={6}>
              {!this.state.isActive ? (
                // <DefaultButton
                //   className="button"
                //   text={t('BTN_DEACTIVATE')}
                //   styles={PIMButtons}
                //   onClick={() => this.setState({ confirmModel: true })}
                //   disabled={this.state.selectionCount > 0 ? false : true}
                // />
                <span title={t('BTN_DEACTIVATE')}>
                  <Icon
                    iconName={allInactive ? 'ActivateOrders' : 'DeactivateOrders'}
                    className={!this.state.isActive ? 'buttonIcon1' : 'buttonIcon1'}
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
              ) : null}

              {this.state.isActive ? (
                <span title={t('BTN_ACTIVATE')}>
                  <Icon
                    iconName={allActive ? 'DeactivateOrders' : 'ActivateOrders'}
                    className={this.state.isActive ? 'buttonIcon1' : 'buttonIcon1'}
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
              ) : null}
            </Grid>
            <Grid>
              {this.roleId.includes(1) && (
                <span title={this.props.t('DELETE_MARKETPLACE')}>
                  <Icon
                    iconName="Delete"
                    className="buttonIcon9"
                    onClick={() => {
                      if (this.state.selectionCount > 0) {
                        // this.DeleteMarketPlace();
                        this.setState({ A_D_Visible1: true });
                      }
                    }}
                    style={{
                      cursor: this.state.selectionCount > 0 ? 'pointer' : 'not-allowed',
                      color: this.state.selectionCount > 0 ? 'inherit' : 'gray',
                    }}
                  />
                </span>
              )}
            </Grid>
            {!this.state.isActive && (
              <Grid item lg={0.9} xs={6}>
                {this.roleId.indexOf('1') > -1 && (
                  <span title={t('BTN_EDIT')}>
                    <Icon
                      iconName="Edit"
                      className="buttonIcon1"
                      onClick={() => {
                        if (this.state.selectionCount == 1) {
                          this.setState({ marketPlace: true });
                        }
                      }}
                      style={{
                        cursor: this.state.selectionCount === 1 ? 'pointer' : 'not-allowed',
                        color: this.state.selectionCount === 1 ? 'inherit' : 'gray',
                      }}
                    />
                  </span>
                )}
              </Grid>
            )}
            <Grid item lg={!this.state.isActive ? 4.3 : 5.2} xs={0} />
            <Grid item lg={0.5} xs={3}>
              <Icon
                iconName="Refresh"
                title={t('BTN_REFRESH')}
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
                    onClick={this.ActivateMarketPlace.bind(this)}
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
            isOpen={this.state.marketPlace}
            containerClassName={PIMcontentStyles.container5}
            onDismiss={() =>
              this.setState({
                marketPlace: false,
              })
            }
          >
            <div className={PIMcontentStyles.confirmHeader}>
              <IconButton
                styles={MarketplaceStyles}
                iconProps={cancelIcon}
                ariaLabel="Close popup modal"
                onClick={() => {
                  this.setState({ 
                    marketPlace: false, 
                    divisionSelected:'' 
                  });
                  this.refresh(2);
                }}
              />
            </div>
            <Grid item xs={10}>
              <AddOrUpdateMarketPlace
                CloseModal={() => {
                  this.setState({ 
                    marketPlace: false, 
                    divisionSelected:'' 
                  }),
                  this.refresh(2);
                }
                }
                fields={this.state.selectedRowData}
                response={this.onResponse}
              />
            </Grid>
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
                    onClick={this.DeleteMarketPlace.bind(this)}
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
              data={this.state.marketData}
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
              fixedHeaderScrollHeight="67vh"
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

const ComponentTranslated = withTranslation()(MarketPlace);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
