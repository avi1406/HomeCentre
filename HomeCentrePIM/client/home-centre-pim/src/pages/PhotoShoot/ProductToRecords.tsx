import { ComboBox, Icon, IconButton, Modal, SearchBox, Stack, TextField } from '@fluentui/react';
import { Grid } from '@mui/material';
import React, { Suspense } from 'react';
import { withTranslation } from 'react-i18next';
import { Text } from '@fluentui/react/lib/Text';
import { columnHeader, PIMcontentStyles, PIMHearderText, PIMsearchBoxStyles } from '../PimStyles';
import DataTable from 'react-data-table-component';
import { ProductImageModel, UidSearchModel } from '@/services/ApiClient';
import NotificationManager from '@/services/NotificationManager';
import AuthManagerService from '@/services/AuthManagerService';
import ApiManager from '@/services/ApiManager';
import axios from 'axios';
import {
  cancelIcon,
  clearFilter,
  columnFilter,
  filterButton,
  filterIcon,
  iconButtonStyles,
  rejectComment,
} from '@/common/properties';
import { FaRegClone } from 'react-icons/fa';
import { baseUrl } from '@/services/Constants';

class ProductToRecords extends React.Component<any, any> {
  constructor(props: any) {
    super(props);
    this.state = {
      Approvaloption: [],
      Division: '',
      division: [],
      divisionOptionlst: [],
      searchKey: '',
      exportvisible: false,
      isDivision: false,
      currentRecord: [],
      divisionSelected: 0,
      isRecord: false,
      dropDownSelectedRecord: [],
      selectedRowData: [],
      noSelection: false,
      actionL1Selected: 0,
      actionL2Selected: 0,
      isACL2: false,
      isACL1: false,
    };
  }
  actionL1: any = [];
  actionL2: any = [];

  authManager = new AuthManagerService();
  notify = new NotificationManager();
  apiClient = new ApiManager().CreateApiClient();
  token = sessionStorage.getItem('token');
  isAuthenticated = this.authManager.isAuthenticated();
  user = this.isAuthenticated ? this.authManager.getUserData() : null;
  deptId: any[] = this.user?.departmentId || [];
  division: any = [];
  PTstatus: any = [];
  filterDivision: any = [];

  async componentDidMount(): Promise<void> {
    this.refresh();
    var departmentList = (await this.apiClient.getDivision())?.result;
    var departmentOption: { key: number | undefined; text: string | undefined }[] = [];
    var deptOpt: { key: number | undefined; text: string | undefined }[] = [];
    departmentList?.forEach((element) => {
      departmentOption.push({ key: element.id, text: element.name });
      deptOpt.push({ key: element.id, text: element.name });
    });
    this.division = [];
    this.setState({
      departmentOption: departmentOption,
      deptOption: deptOpt,
      division: [],
      isACL1: false,
    });
  }

  async refresh() {
    this.setState({ isRecord: true });
    var body = new UidSearchModel();
    try {
      this.setState({ searchKey: '' });
      const response: any = (await this.apiClient.getPlanningDetails()).result;
      const filteredResponse = response?.filter((item: any) =>
        this.deptId.includes(item.divisionId),
      );
      const responeWithIndex = filteredResponse.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      }));
      var PhotoshootCAM = (await this.apiClient.photoshootstatus())?.result;
      var PhotoSstatus: { key: number | undefined; text: string | undefined }[] = [];
      PhotoshootCAM?.forEach((element: any) => {
        PhotoSstatus.push({ key: element.id, text: element.name });
      });
      this.setState({
        filteredRecord: responeWithIndex,
        currentRecord: responeWithIndex,
        PSstatus: PhotoSstatus,
        isRecord: true,
        exportvisible: responeWithIndex.length !== 0 ? true : false,
        noSelection: !this.state.noSelection,
      });
    } catch (e: any) {
      this.notify.showErrorNotify(e.message);
    }
    this.actionL1 = [];
    this.actionL2 = [];
    this.filterDivision = [];
    this.setState({
      isDivision: false,
      exportvisible: this.state.currentRecord.length !== 0 ? true : false,
      dropDownSelectedRecord: this.state.currentRecord,
      filteredRecord: this.state.currentRecord,
      selectedRowData: [],
      actionL1: [],
      actionL2: [],
      isACL1: false,
      divisionSelected: 0,
      isRecord: false,
      actionL2Selected: 0,
    });
  }
  downloadFile = () => {
    var body = new ProductImageModel();
    const filteredDeptId = this.deptId.filter((id) => id !== 0);
    const commaSeparatedString = filteredDeptId.join(',');
    body.userDivision = commaSeparatedString;
    this.notify.showSuccessNotify1(this.props.t('DOWNLOAD_STARTED'));
    document.body.classList.add('loading-indicator');
    axios({
      data: body,
      url: `${baseUrl}/api/Planning/PlanningExportExcel`,
      method: 'POST',
      responseType: 'blob',
    })
      .then((response: any) => {
        document.body.classList.remove('loading-indicator');
        this.notify.hideErrorNotify();
        this.notify.showSuccessNotify(this.props.t('DOWNLOAD_COMPLETED'));
        const url = window.URL.createObjectURL(new Blob([response.data]));
        const link = document.createElement('a');
        link.href = url;
        link.setAttribute('download', 'GRNRecords.xlsx');
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
      })
      .catch((error: any) => {
        this.notify.showErrorNotify(error);
      });
  };

  DivisionFilter = async () => {
    //debugger;
    //var secondaryFilter: any = [];
    var finalData: any = [];
    if (this.filterDivision.length != 0) {
      finalData = this.state.currentRecord.filter((element: any) => {
        return element.division == this.filterDivision;
      });
    } else {
      finalData = this.state.currentRecord;
    }
    // if (this.actionL1.length != 0) {
    //   secondaryFilter = finalData.filter((element: any) => {
    //     return element.planoOrFamilyNameCreationStatusL1 == this.actionL1;
    //   });
    // } else {
    //   secondaryFilter = finalData;
    // }

    if (this.actionL2.length != 0) {
      if (this.actionL2 == 'Pending' || this.actionL2 == '1') {
        finalData = finalData.filter((element: any) => {
          return element.photoShootStatus == 'Pending';
        });
      } else {
        finalData = finalData.filter((element: any) => {
          return element.photoShootStatus == this.actionL2;
        });
      }
    }
    // else {
    //   finalData = secondaryFilter;
    // }

    this.setState({
      dropDownSelectedRecord: finalData,
      filteredRecord: finalData,
      exportvisible: finalData.length !== 0 ? true : false,
    });
  };

  onSearch(e: any) {
    var newValue = e?.target.value;
    newValue = newValue == undefined ? '' : newValue;
    var { dropDownSelectedRecord } = this.state;
    var searchResult = dropDownSelectedRecord.filter((element: any) => {
      return element?.itemCode.toString().includes(newValue.trim());
    });
    this.setState({
      filteredRecord: searchResult.map((item: any, index: any) => ({ index: index + 1, ...item })),
      searchKey: newValue,
      exportvisible: searchResult.length !== 0,
    });
  }

  render() {
    const { t } = this.props;
    console.log(this.state.filteredRecord);
    var columns: any = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{this.props.t('S_NO')}</div>,
        selector: (row: any) => row.index,
        width: '70px',
      },
      {
        key: 'itemcode',
        name: <div className={columnHeader}>{this.props.t('ITEMCODE')}</div>,
        selector: (row: any) => <span title={row.itemCode}>{row.itemCode}</span>,
        width: '140px',
      },
      {
        key: 'eancode',
        name: <div className={columnHeader}>{`${this.props.t('COL_EAN_CODE')}`}</div>,
        selector: (row: any) => <span title={row.eanCode}>{row.eanCode}</span>,
        minWidth: '140px',
      },
      {
        key: 'grndate',
        name: <div className={columnHeader}>{this.props.t('COL_GRN_DATE')}</div>,
        selector: (row: any) => <span title={row.grnDate}>{row.grnDate}</span>,
        width: '120px',
      },
      {
        key: 'division',
        name: (
          <Grid container direction={'row'}>
            <Grid item lg={8} xs={9} style={{ marginTop: '6px' }}>
              <div className={columnHeader}>{this.props.t('PLANOGRAM_TABLE_DIVISION')}</div>
            </Grid>
            <Grid item lg={2} xs={3}>
              <ComboBox
                className="dropdown"
                options={this.state.departmentOption}
                styles={columnFilter}
                onChange={(e: any, selectedOption: any) => {
                  this.filterDivision = selectedOption.text;
                  this.setState({
                    isDivision: true,
                    divisionSelected: selectedOption.key,
                    // searchKey: '',
                  });
                  this.DivisionFilter();
                }}
                selectedKey={this.state.divisionSelected}
                style={{ marginLeft: -25, marginTop: 4 }}
              />
              <IconButton
                style={{ marginLeft: -30 }}
                styles={filterButton}
                iconProps={filterIcon}
                title={'Filter'}
                ariaLabel="Close popup modal"
              />
            </Grid>
            <Grid item lg={1} xs={1}>
              {this.state.isDivision && (
                <IconButton
                  style={{ marginLeft: -5 }}
                  title={'Clear Filter'}
                  styles={filterButton}
                  onClick={() => {
                    this.filterDivision = [];
                    this.setState({ isDivision: false, divisionSelected: 0 });
                    this.DivisionFilter();
                  }}
                  iconProps={clearFilter}
                />
              )}
            </Grid>
          </Grid>
        ),
        position: 'fixed',
        selector: (row: any) => <span title={row.division}>{row.division}</span>,
        width: '130px',
      },
      {
        key: 'productDepartment',
        name: <div className={columnHeader}>{this.props.t('COL_GRN_LOCATION')}</div>,
        selector: (row: any) => <span title={row.grnLocation}>{row.grnLocation}</span>,
      },
      {
        key: 'fromloc',
        name: <div className={columnHeader}>{this.props.t('COL_FROM_LOC')}</div>,
        selector: (row: any) => <span title={row.from_Loc}>{row.from_Loc}</span>,
      },
      {
        key: 'toloc',
        name: <div className={columnHeader}>{this.props.t('COL_TO_LOC')}</div>,
        selector: (row: any) => <span title={row.to_Loc}>{row.to_Loc}</span>,
      },
      // {
      //   key: 'masterAttributesId',
      //   name: <div className={columnHeader}>{this.props.t('COL_TRANSFER_ORDER')}</div>,
      //   selector: (row: any) => <span title={row.toto}>{row.toto}</span>,
      // },
      // {
      //   key: 'marketplaceId',
      //   name: <div className={columnHeader}>{this.props.t('COL_TO_DESTN')}</div>,
      //   selector: (row: any) => <span title={row.grnlocation}>{row.grnlocation}</span>,
      // },
      {
        key: 'marketplace',
        name: <div className={columnHeader}>{this.props.t('COL_TO_DATE')}</div>,
        selector: (row: any) => <span title={row.tO_Date}>{row.tO_Date}</span>,
      },
      {
        key: 'allocationstudio',
        name: <div className={columnHeader}>{this.props.t('COL_ALLOCATION_STUDIO')}</div>,
        selector: (row: any) => <span title={row.studio}>{row.studio}</span>,
      },
      // {
      //   key: 'productDepartment',
      //   name: <div className={columnHeader}>{this.props.t('COL_REV_TO')}</div>,
      //   selector: (row: any) => <span title={row.productDepartmentId}>{row.productDepartmentId}</span>,
      // },
      // {
      //   key: 'masterAttributesId',
      //   name: <div className={columnHeader}>{this.props.t('COL_REV_TO_DATE')}</div>,
      //   selector: (row: any) => <span title={row.grndate}>{row.grndate}</span>,
      // },
      // {
      //   key: 'masterAttributesId',
      //   name: <div className={columnHeader}>{this.props.t('COL_REV_TO_DESTN')}</div>,
      //   selector: (row: any) => <span title={row.grnlocation}>{row.grnlocation}</span>,
      // },
    {
      key: 'productDepartmentId',
      name: <div className={columnHeader}>{this.props.t('COL_STATUS')}</div>,
      selector: (row: any) => {
          const status = row.productToStatus;
          let statusClass = '';
  
          if (status === 'WIP') {
              statusClass = 'customer-badge status-Pending';
          } else if (status === 'Approved' || status === 'Submitted') {
              statusClass = 'customer-badge status-Approved';
          }
          return (
              <span className={statusClass} title={status}>{status}</span>
          );
        },
    },
      {
        key: 'photoshoot_status',
        name: (
          <Grid container direction={'row'}>
            <Grid item lg={8} xs={9}>
              <div className={columnHeader}>{`${this.props.t('COL_PHOTOSHOOT_STATUS')}`}</div>
            </Grid>
            <Grid item lg={2} xs={3}>
              <ComboBox
                className="dropdown"
                options={this.state.PSstatus}
                onChange={(e: any, selectedOption: any) => {
                  this.actionL2 = selectedOption.text;
                  this.setState({
                    isACL1: true,
                    actionL2Selected: selectedOption.key,
                    searchKey: '',
                    //PSstatusopt1: selectedOption.key
                  });
                  this.DivisionFilter();
                }}
                selectedKey={this.state.actionL2Selected}
                styles={columnFilter}
                style={{ marginLeft: -22, marginTop: 18 }}
              />
              <IconButton
                style={{ marginLeft: -27, marginTop: 8 }}
                styles={filterButton}
                iconProps={filterIcon}
                title={'Filter'}
                ariaLabel="Close popup modal"
              />
            </Grid>
            <Grid item lg={1} xs={1}>
              {this.state.isACL1 && (
                <IconButton
                  style={{ marginLeft: -3, marginTop: 8 }}
                  title={'Clear Filter'}
                  styles={filterButton}
                  iconProps={clearFilter}
                  onClick={() => {
                    this.actionL2 = '';
                    this.setState({ isACL1: false, actionL2Selected: 0 });
                    this.DivisionFilter();
                  }}
                />
              )}
            </Grid>
          </Grid>
        ),
        selector: (row: any) => {
          if (this.state.filteredRecord != null && this.state.filteredRecord != undefined) {
            if (row.photoShootStatus === 'Rejected') {
              return (
                <span className={`customer-badge status-Rejected`}>
                  <span title={`${'Rejected'}`}>{'Rejected'}</span>
                </span>
              );
            } else {
              if (row.photoShootStatus === 'Completed') {
                return (
                  <span className={`customer-badge status-Approved`}>
                    <span title={`${'Completed'}`}>{'Completed'}</span>
                  </span>
                );
              } else {
                if (row.photoShootStatus === 'Pending') {
                  return (
                    <span className={'customer-badge status-Pending'}>
                      <span title={`${'Pending'}`}>{'Pending'}</span>
                    </span>
                  );
                } else {
                  return <span></span>;
                }
              }
            }
          }
        },
        isVisible: true,
        minWidth: '130px',
      },
      {
        key: 'stock_return_to_qc',
        name: <div className={columnHeader}>{`${this.props.t('COL_STOCK_RETURN')}`}</div>,
        isVisible: true,
        minWidth: '120px',
        selector: (row: any) => {
          switch (row.stockReturnToQc) {
            case '0':
              return <span title={`${'Yet To Return'}`}>{'Yet To Return'}</span>;
            case '1':
              return <>
                <span title={`${'Returned'}`}>{'Returned'}</span>
                <span style={{ cursor: 'pointer', marginLeft: '5px' }}>
                  {row.photoshootComment != null ? (
                    <FaRegClone
                      onClick={() => {
                        this.setState({
                          returnstatus: true,
                          returnvalue: row.photoshootComment,
                        });
                      }}
                    />
                  ) : (
                    ''
                  )}
                </span>
              </>;
            default:
              return <span></span>;
          }
        },
      },
      {
        key: 'CamQcStatus',
        name: <div className={columnHeader}>{this.props.t('COL_CAM_STATUS')}</div>,
        selector: (row: any) => {
            const status = row.camQcStatus;
            let displayText;
            let statusClass = '';
    
            if (status === 'Pass') {
                displayText = 'Approved';
                statusClass ='customer-badge status-Submitted'
            } else if (status === null) {
                displayText = '';
            } else {
                displayText = status;
            }
            return <span title={displayText} className={statusClass}>{displayText}</span>;
        },
    },    
    ];

    const handleColumnReorder = (newColumns: any) => {
      var _column: any = [...columns];
      if (newColumns == this.state.currentColumns) {
        this.setState({ currentColumns: _column });
      } else {
        this.setState({ currentColumns: newColumns });
      }
    };

    return (
      <div className="layout width-100">
        <div className="bg-Color">
          <Grid container spacing={1} direction="row" alignItems="center" className="grid">
            <Grid item lg={10.6} xs={8}>
              <Text
                variant="xxLarge"
                styles={PIMHearderText}
                className="color-blue text"
              >{`${this.props.t('MENU_PRODUCT_TO_RECORDS')}`}</Text>
            </Grid>
            <Grid item lg={0.94} xs={2} />
            <Grid item lg={0.1} xs={1}>
              <Icon
                iconName="Download"
                className="iconStyle"
                title={this.props.t('BTN_DOWNLOAD')}
                onClick={() => {
                  if (this.state.filteredRecord.length !== 0) {
                    this.downloadFile();
                  }
                }}
              />
              {/* <Grid item lg={0.5} xs={2} /> */}
            </Grid>
          </Grid>

          <div className="QcModel" />

          <Grid lg={12} item container spacing={2} direction={'row'}>
            <Grid item lg={2} xs={6}>
              <SearchBox
                placeholder={this.props.t('ITEMCODE')}
                styles={PIMsearchBoxStyles}
                value={this.state.searchKey}
                onChange={(e: any) => this.onSearch(e)}
                onClear={() => this.setState({ searchKey: '' })}
              />
            </Grid>
            <Grid item lg={8.95} xs={7.8}></Grid>

            <Grid item lg={0.5} xs={1}>
              <Icon
                iconName="Clear"
                title={t('BTN_CLEAR')}
                className="iconStyle"
                onClick={() => {
                  if (this.state.searchKey != '')
                    // this.setState({ searchKey: '' });
                    this.refresh();
                }}
              />
            </Grid>
            <Grid item lg={0.5} xs={1.8}>
              <Icon
                iconName="Refresh"
                title={this.props.t('BTN_REFRESH')}
                className="iconStyle"
                onClick={() => this.refresh()}
              />
            </Grid>
            <Grid item lg={0.4} xs={1.8} className="spacegrid" />
          </Grid>

          <Modal
            isOpen={this.state.returnstatus}
            containerClassName={PIMcontentStyles.confirmContainer}
            onDismiss={() => this.setState({ returnstatus: false })}
            isBlocking={false}
          >
            <div className="statusCommentscancel">
              {/* <div className="apptext3"> {`${this.props.t('PLANOGRAM_MODEL_LABEL_REJECTED')}`}</div> */}
              <IconButton
                styles={iconButtonStyles}
                iconProps={cancelIcon}
                ariaLabel="Close popup modal"
                onClick={() => {
                  this.setState({ returnstatus: false });
                }}
              />
            </div>
            <div className="statusComments">
              <form noValidate autoComplete="off">
                <TextField
                  styles={rejectComment}
                  className="textField"
                  label={`${this.props.t('comment')}`}
                  value={this.state.returnvalue}
                  name="comment"
                  required
                  resizable={false}
                  multiline
                  autoAdjustHeight
                  data-attribute-name="Name"
                  data-async
                />
              </form>
            </div>
          </Modal>

          <Modal
            isOpen={this.state.recjected}
            containerClassName={PIMcontentStyles.confirmContainer}
            onDismiss={() => this.setState({ recjected: false })}
            isBlocking={false}
          >
            <div className="statusCommentscancel">
              {/* <div className="apptext3"> {`${this.props.t('PLANOGRAM_MODEL_LABEL_REJECTED')}`}</div> */}
              <IconButton
                styles={iconButtonStyles}
                iconProps={cancelIcon}
                ariaLabel="Close popup modal"
                onClick={() => {
                  this.setState({ recjected: false });
                }}
              />
            </div>
            <div className="statusComments">
              <form noValidate autoComplete="off">
                <TextField
                  styles={rejectComment}
                  className="textField"
                  label={`${this.props.t('comment')}`}
                  value={this.state.descriptionValue}
                  name="comment"
                  required
                  resizable={false}
                  multiline
                  autoAdjustHeight
                  data-attribute-name="Name"
                  data-async
                />
              </form>
            </div>
          </Modal>
          <div className='table1 dataTableFixedFooter'>
            <DataTable
              columns={columns}
              data={this.state.filteredRecord}
              selectableRows={this.state.selectedRowData}
              clearSelectedRows={this.state.noSelection}
              pagination={true}
              selectableRowsHighlight
              fixedHeader
              fixedHeaderScrollHeight="68.3vh"
              onColumnOrderChange={handleColumnReorder}
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
const ComponentTranslated = withTranslation()(ProductToRecords);
function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}
export default App;
