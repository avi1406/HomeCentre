import {
  DefaultButton,
  Dropdown,
  IComboBoxOption,
  Icon,
  IconButton,
  Modal,
  SearchBox,
  Stack,
  Image,
  TextField,
  ComboBox,
} from '@fluentui/react';
import React, { Suspense } from 'react';
import DataTable from 'react-data-table-component';
import { Text } from '@fluentui/react/lib/Text';
import { Grid } from '@mui/material';
import ApiManager from '@/services/ApiManager';
import AuthManagerService from '@/services/AuthManagerService';
import { withTranslation } from 'react-i18next';
import jwt_decode from 'jwt-decode';
import NotificationManager from '@/services/NotificationManager';
import axios from 'axios';
import {
  columnHeader,
  PIMcontentStyles,
  PIMdropdownStyles2,
  PIMsearchBoxStyles,
  PIMfooterButtonCancel,
  PIMfooterButtonConfirm,
  PIMdropdownStylesQCTab,
  PIMdropdownStylesDd,
  PIMdropdownStyles21,
} from '../PimStyles';
import {
  approveIcon,
  cancelIcon,
  clearFilter,
  columnFilter,
  filterButton,
  filterIcon,
  iconButtonStyles,
  iconButtonStyless,
  iconButtonStyless1,
  iconButtonStylesssClicked,
  iconButtonStylesssClicked1,
  rejectComment,
  rejectIcon,
  UploadStyles,
} from '@/common/properties';
import { baseUrl } from '@/services/Constants';
import CommonDate, { CommonDateRef } from '../../pages/commonFunctions';
import Util from '@/services/Util';
import { FaRegClone } from 'react-icons/fa';
import { ReceivedProductsModel } from '@/services/ApiClient';

class ReceivedProducts extends React.Component<any, any> {
  location = window.location.pathname;
  notify = new NotificationManager();
  apiClient = new ApiManager().CreateApiClient();
  utils = new Util();
  authManager = new AuthManagerService();
  isAuthenticated = this.authManager.isAuthenticated();
  user = this.isAuthenticated ? this.authManager.getUserData() : null;
  roleId: any[] = this.user?.roleId || [];
  deptId: any[] = this.user?.departmentId || [];
  username: any = this.user?.name || '';
  token = sessionStorage.getItem('token');
  decode: any = jwt_decode(this.token || '');
  fieldType: IComboBoxOption[] | undefined;
  selectedRowData: null | undefined;

  commonDateRefs: React.RefObject<CommonDateRef>[] = Array.from({ length: 2 }, () =>
    React.createRef<CommonDateRef>(),
  );
  statuss: any;
  TypeColText: any = [];

  constructor(props: any) {
    super(props);
    this.state = {
      divitionValue: this.props.division || null,
      currentRecord: [],
      searchKey: '',
      selectedRowData: [],
      noSelection: false,
      row: [],
      dropDownSelectedRecord: [],
      searchResult: [],
      filteredRecord: [],
      minDate: undefined,
      minDate1: undefined,
      toDate: undefined,
      toDate1: undefined,
      isRecord: true,
      exportvisible: false,
      statuss: [],
      A_D_Visible: false,
      imgVisible: false,
      imageURL: '',
      pictureData: [],
      recjected: false,
      handoverOptions1: [],
      refreshKey: 0,
      deptOption: [],
      selecteddept: [],
      otherSplitComment: '',
      dropdown: '',
      divisionSelected: '',
    };
  }

  async componentDidMount(): Promise<void> {
    var handoverStatus = (await this.apiClient.getHandOverStatus())?.result;
    var handoverOption: { key: number | undefined; text: string | undefined }[] = [];
    handoverStatus?.forEach((element) => {
      handoverOption.push({ key: element.id, text: element.name });
    });
    this.refresh();
    var departmentList = (await this.apiClient.getDivision())?.result;
    var departmentOption: { key: number | undefined; text: string | undefined }[] = [];
    var deptOpt: { key: number | undefined; text: string | undefined }[] = [];
    const filteredDeptId = this.deptId.filter((id) => id !== 0);
    // deptOpt.push({ key: 1, text: 'All' });
    // departmentList?.forEach((element) => {
    //   departmentOption.push({ key: element.id, text: element.name });
    //   deptOpt.push({ key: element.id, text: element.name });
    // });

    // filteredDeptId.forEach((id) => {
    //   const matchingDepartment = departmentList?.find((dept) => dept.id === id);
    //   if (matchingDepartment) {
    //     departmentOption.push({ key: matchingDepartment.id, text: matchingDepartment.name });
    //     deptOpt.push({ key: matchingDepartment.id, text: matchingDepartment.name });
    //   }
    // });
    departmentList?.forEach((dept) => {
      const matchingId = filteredDeptId?.find((id) => id === dept.id);
      const matchingDepartment = departmentList?.find((dept) => dept.id === matchingId);
      if (matchingDepartment) {
        departmentOption.push({ key: matchingDepartment.id, text: matchingDepartment.name });
        deptOpt.push({ key: matchingDepartment.id, text: matchingDepartment.name });
      }
    });

    var ProductImageType = (await this.apiClient.getProductImageType())?.result;
    var ProductImageoption: { key: number | undefined; text: string | undefined }[] = [];
    ProductImageType?.forEach((element) => {
      if (element.name !== 'All') {
        ProductImageoption.push({ key: element.id, text: element.name });
      }
    });

    this.setState({
      handoverOptions1: handoverOption,
      ProductImageoptions: ProductImageoption,
      deptOption: deptOpt,
    });
  }

  async refresh() {
    this.setState({ refreshKey: this.state.refreshKey + 1, refresh1: 0, selecteddept: [] });
    this.TypeColText = [];
    try {
      this.setState({ searchKey: '' });
      const response: any = (await this.apiClient.getQcRecords()).result;
      const filteredResponse = response?.filter((item: any) =>
        this.deptId.includes(item.divisionId),
      );
      const responeWithIndex = filteredResponse.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      }));
      this.setState({
        filteredRecord: [],
        otherSplitComment:''
      });
      this.setState({
        divisionSelected: '',
        pictureData: response,
        currentRecord: responeWithIndex,
        dropDownSelectedRecord: responeWithIndex,
        filteredRecord: responeWithIndex,
        isRecord: this.state.filteredRecord.length > 0 ? true : false,
        exportvisible: responeWithIndex.length !== 0 ? true : false,
        DisplayRecordState:
          response.length === 0 ? (
            <h5 style={{ paddingTop: '2%' }}>{this.props.t('NO_RECORDS')}</h5>
          ) : (
            <h5 style={{ paddingTop: '10%' }}>{this.props.t('LOADING_RECORDS')}</h5>
          ),
      });
    } catch (e: any) {
      this.notify.showErrorNotify(e.message);
    }

    this.setState({
      noSelection: !this.state.noSelection,
      selectedRowData: [],
      TypeColKey: '',
      selectionCount: 0,
      minDate: undefined,
      minDate1: undefined,
      onClickToDate: undefined,
      onClickMinDate: undefined,
      toDate: undefined,
      toDate1: undefined,
      dropDownSelectedRecord: this.state.currentRecord,
      filteredRecord: this.state.currentRecord,
      exportvisible: this.state.currentRecord.length !== 0 ? true : false,
      refresh1: this.state.refresh1 + 1,
    });
    this.commonDateRefs.forEach((ref) => {
      if (ref.current) {
        ref.current.resetDates();
      }
    });
  }
  resetDates = () => {
    this.setState({
      minDate: undefined,
      toDate: undefined,
    });
  };
  Filterdata = () => {
    if (this.state.selecteddept.length != 0) {
      var selectedDeptname = this.state.selecteddept.map((selectedDept: any) => selectedDept);
      if (selectedDeptname.length !== this.state.deptOption.length) {
        var ids: any = [];
        var id;
        selectedDeptname.forEach((selectedDept: any) => {
          var option = this.state.deptOption.find((option: any) => {
            return selectedDept === option.text;
          });
          if (option) {
            id = option;
            ids.push(option.key);
          }
        });
        if (ids.length > 0) {
          var record = this.state.currentRecord.filter((e: any) => {
            return ids.includes(e.divisionId);
          });

          this.setState({ filteredRecord: record, Dropdownvalue: id });
        }
      } else {
        this.setState({ filteredRecord: this.state.currentRecord });
      }
    } else {
      this.setState({ filteredRecord: this.state.currentRecord });
    }
  };
  onSearch(e: any) {
    var newValue = e?.target.value;
    newValue = newValue == undefined ? '' : newValue;
    var { currentRecord } = this.state;
    var searchResult = currentRecord.filter((element: any) => {
      return element?.itemCode.toString().includes(newValue.trim());
    });
    this.setState({ filteredRecord: searchResult, searchKey: newValue });
    this.setState({
      filteredRecord: searchResult.map((item: any, index: any) => ({ index: index + 1, ...item })),
      exportvisible: searchResult.length !== 0 ? true : false,
    });
  }
  async onApproveBatch() {
    let { selectedRowData } = this.state;
    var itemcode: any[] = [];

    var status = 'true';
    selectedRowData.forEach((element: any) => {
      const recordid = element.itemCode;
      itemcode.push(recordid);
    });
    const response: any = (await this.apiClient.updateQCRecords(itemcode, status, this.username))
      .result;
    const responeWithIndex = response.map((item: any, index: any) => ({
      index: index + 1,
      ...item,
    }));
    if (response.hasError) {
      this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN') || response.message);
    } else {
      this.setState({
        ApprovalEnable: true,
        ApproveSelectedClicked: false,
        QcRecord: responeWithIndex,
      });

      this.notify.showSuccessNotify(this.props.t('APPROVED_SUCESSFULLY'));
    }
    this.refresh();
  }

  columnFilter1 = async () => {
    var finalData: any = [];
    //var finalData: any = this.state.filteredRecord;

    if (this.TypeColText.length !== 0) {
      finalData = this.state.currentRecord.filter((element: any) => {
        return element.imageType == this.TypeColText;
      });
    } else {
      finalData = this.state.currentRecord;
    }

    this.setState({
      filteredRecord: finalData,
    });
  };

  async onFilterClick() {
    var { toDate, minDate, minDate1, toDate1, divisionSelected } = this.state;
    var fromDate = '';
    var receivedtoDate = '';
    var receivedfromDate = '';
    var handOvertoDate = '';
    var handOverfromDate = '';
    let commaSeparatedString;
    if (this.state.selecteddept.length == 0) {
      let filteredData = this.deptId.filter((item) => item !== 0);
      commaSeparatedString = filteredData.join(',');
    } else {
      commaSeparatedString = this.state.selecteddept.map((divDeptText: string) => {
        const division = this.state.deptOption.find((e: any) => e.text === divDeptText);
        return division ? division.key : null;
      });
    }
    var status = this.state.divisionSelected;
    var data;
    if (status == 'Sent') {
      data = 'Approved';
    } else if (status == 'Not Sent') {
      data = 'Rejected';
    }
    if (minDate != undefined) {
      const dateParts = new Date(minDate).toLocaleDateString('en-CA').split('/');
      fromDate = dateParts[0];
      receivedfromDate = fromDate;
    }
    if (toDate != undefined) {
      const dateParts = new Date(toDate).toLocaleDateString('en-CA').split('/');
      toDate = dateParts[0];
      receivedtoDate = toDate;
    }
    if (minDate1 != undefined) {
      const dateParts = new Date(minDate1).toLocaleDateString('en-CA').split('/');
      fromDate = dateParts[0];
      handOverfromDate = fromDate;
    }
    if (toDate1 != undefined) {
      const dateParts = new Date(toDate1).toLocaleDateString('en-CA').split('/');
      toDate = dateParts[0];
      handOvertoDate = toDate;
    }
    if (status != undefined) status = divisionSelected;
    const response = (
      await this.apiClient.getAllReceivedProductQCFilters(
        data,
        receivedfromDate,
        receivedtoDate,
        handOverfromDate,
        handOvertoDate,
        commaSeparatedString,
      )
    ).result;
    const filteredResponse = response?.filter((item: any) => this.deptId.includes(item.divisionId));
    const responeWithIndex = filteredResponse?.map((item: any, index: any) => ({
      index: index + 1,
      ...item,
    }));
    this.setState(
      {
        searchKey: '',
        onClickMinDate: minDate,
        onClickToDate: toDate,
        currentRecord: responeWithIndex,
        dropDownSelectedRecord: responeWithIndex,
        filteredRecord: responeWithIndex,
        exportvisible: responeWithIndex?.length !== 0 ? true : false,
      },
      () => {
        if (responeWithIndex?.length != 0) {
          this.Filterdata();
        }
      },
    );
  }

  downloadFileQC(
    receivedfromDate: any,
    receivedtoDate: any,
    handOverfromDate: any,
    handOvertoDate: any,
    status: any,
    division: any,
  ) {
    var body = new ReceivedProductsModel();
    body.handOverFromDate = this.state.minDate1?.toLocaleDateString('en-CA');
    body.handOverToDate = this.state.toDate1?.toLocaleDateString('en-CA');
    body.receivedFromDate = this.state.minDate?.toLocaleDateString('en-CA');
    body.receivedToDate = this.state.toDate?.toLocaleDateString('en-CA');
    body.division = this.state.selecteddept.toString();
    const filteredDeptId = this.deptId.filter((id) => id !== 0);
    const commaSeparatedString = filteredDeptId.join(',');
    body.userDivision = commaSeparatedString;
    var data;
    if (this.state.divisionSelected === 'Sent') {
      data = 'Approved';
    } else if (this.state.divisionSelected === 'Not Sent') {
      data = 'Rejected';
    } else {
      data = this.state.divisionSelected;
    }
    body.status = data;
    this.notify.showSuccessNotify1(this.props.t('DOWNLOAD_STARTED'));
    document.body.classList.add('loading-indicator');
    axios({
      data: body,
      url: `${baseUrl}/api/ProductImage/QCExportExcel`,
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
        link.setAttribute('download', 'QCApprovalExcel.xlsx');
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
      })
      .catch((error: any) => {
        this.notify.showErrorNotify(error);
      });
  }
  rowSelection = (e: any) => {
    const selectionCount = e.selectedCount;
    this.setState({ selectedRowData: e.selectedRows, selectionCount: selectionCount });
    switch (selectionCount) {
      case 0:
        this.setState({ enableEditButton: false, enableActiveButton: false, ApprovalEnable: true });
        break;
      case 1:
        this.setState({ enableEditButton: true, enableActiveButton: true, ApprovalEnable: true });
        break;
      default:
        this.setState({ enableEditButton: false, enableActiveButton: true });
        break;
    }
    if (selectionCount >= 2) {
      this.setState({ ApprovalEnable: false });
    }
  };

  handleMinDateChange = (date: any) => {
    this.setState({ minDate: date, receivedfromDate: date });
  };
  handleMinDateChange1 = (date: any) => {
    this.setState({ minDate1: date, handOverfromDate: date });
  };
  perpage = () => {
    const recordCont = [25, 50, 100, 150, 200];
    return recordCont;
  };
  Defaultperpage = () => {
    const recordCont = 25;
    return recordCont;
  };
  handleToDateChange = (date: any) => {
    this.setState({ toDate: date, receivedtoDate: date });
  };
  handleToDateChange1 = (date: any) => {
    this.setState({ toDate1: date, handOvertoDate: date });
  };
  handleRowClick = (row: any) => {
    this.setState({
      A_D_Visible: true,
      selectedRowData: [row],
    });
  };
  onRejectorAprove = async () => {
    try {
      this.setState({ recjectVisible: false, approveVisible: false });
      var itemcode = this.state.itemcode;
      const Itemcode = parseInt(this.state.itemcode, 10);
      var samplestatusrejection = this.state.otherSplitComment;
      var status = 'QC';
      let response;
      if (samplestatusrejection == '') {
        status = 'Approved';
        response = await this.apiClient.updateQCAction(
          itemcode,
          status,
          samplestatusrejection,
          this.username,
        );
      } else {
        response = await this.apiClient.rejectedItemcode(
          Itemcode,
          status,
          samplestatusrejection,
          this.username,
        );
      }
      if (response.hasError) {
        this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN') || response.message);
      } else {
        if (samplestatusrejection == '') {
          this.notify.showSuccessNotify(this.props.t('APPROVED_SUCESSFULLY'));
        } else {
          this.notify.showSuccessNotify(this.props.t('REJECTED_SUCESSFULLY'));
        }
      }
      this.refresh();
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
  };

  render() {
    const { t } = this.props;
    const { deptOption } = this.state;
    const selecteddept =
      deptOption && deptOption.length
        ? this.state.selecteddept.length === deptOption.length
        : false;
    var _columns: any = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{`${this.props.t('S_NO')}`}</div>,
        selector: (row: any) => row.index,
        isVisible: true,
        width: '70px',
      },
      {
        key: 'itemCode',
        name: <div className={columnHeader}>{`${this.props.t('COL_ITEMCODE')}`}</div>,
        selector: (row: any) => <span title={row.itemCode}>{row.itemCode}</span>,
        width: '140px',
        isVisible: true,
      },
      {
        key: 'EANCode',
        name: <div className={columnHeader}>{`${this.props.t('COL_EAN_CODE')}`}</div>,
        selector: (row: any) => <span title={row.eanCode}>{row.eanCode}</span>,
        isVisible: true,
        width: '140px',
      },
      {
        key: 'action',
        name: <div className={columnHeader}>{`${this.props.t('COL_APPROVE_REJECTION')}`}</div>,
        selector: (row: any) => {
          if (row) {
            if (row.sampleStatus === null) {
              row.sampleStatus = 'Pending';
            }
            return (
              <>
                {row && (
                  <Grid container direction={'row'} spacing={2}>
                    {(row.sampleStatus == 'Approved' || row.sampleStatus == 'Pending') && (
                      <Grid item>
                        <IconButton
                          styles={
                            row.sampleStatus !== 'Pending'
                              ? row.sampleStatus == 'Approved'
                                ? iconButtonStylesssClicked
                                : iconButtonStyless
                              : iconButtonStylesssClicked1
                          }
                          iconProps={approveIcon}
                          //disabled={row.sampleStatus == 'Approved'}
                          onClick={() => {
                            if (row.sampleStatus == 'Pending') {
                              this.setState({
                                itemcode: row.itemCode,
                                approveVisible: true,
                                recjectVisible: false,
                                row: row,
                              });
                            }
                          }}
                        />
                      </Grid>
                    )}
                    {(row.sampleStatus == 'Rejected' || row.sampleStatus == 'Pending') && (
                      <Grid item>
                        <IconButton
                          styles={
                            row.sampleStatus !== 'Pending'
                              ? row.sampleStatus == 'Rejected'
                                ? iconButtonStyless1
                                : iconButtonStylesssClicked
                              : iconButtonStyless
                          }
                          iconProps={rejectIcon}
                          onClick={() => {
                            if (row.sampleStatus == 'Pending') {
                              this.setState({
                                itemcode: row.itemCode,
                                row: row,
                                recjectVisible: true,
                              });
                            }
                          }}
                          //disabled={row.sampleStatus == 'Rejected'}
                        />
                      </Grid>
                    )}
                  </Grid>
                )}
              </>
            );
          }
          return null;
        },
        isVisible: true,
        width: '170px',
      },
      {
        key: 'division',
        name: <div className={columnHeader}>{this.props.t('PLANOGRAM_TABLE_DIVISION')}</div>,
        position: 'fixed',
        selector: (row: any) => {
          if (row.divisionId) {
            var division1 = null;
            const DivKey = parseInt(row.divisionId, 10);
            const statusObject = this.state.deptOption
              ? this.state.deptOption.find((element: any) => element.key === DivKey)
              : null;
            if (statusObject) {
              division1 = statusObject.text;
            }
            return (
              <>
                <span title={division1}>{division1}</span>
              </>
            );
          }
        },
        width: '140px',
      },
      {
        key: 'type',
        name: (
          <Grid container direction={'row'} style={{ display: 'flex', alignItems: 'center' }}>
            <Grid item lg={8} xs={9}>
              <div className={columnHeader}>{`${this.props.t('TYPE')}`}</div>
            </Grid>
            <Grid item lg={2} xs={3}>
              <ComboBox
                className="dropdown"
                options={this.state.ProductImageoptions}
                onChange={(e: any, selectedOption: any) => {
                  this.TypeColText = selectedOption.text;
                  this.setState({
                    isDivision: true,
                    TypeColKey: selectedOption.key,
                    searchKey: '',
                  });
                  this.columnFilter1();
                }}
                selectedKey={this.state.TypeColKey}
                styles={columnFilter}
                style={{ marginLeft: -15, marginTop: 10 }}
              />
              <IconButton
                style={{ marginLeft: -20 }}
                styles={filterButton}
                iconProps={filterIcon}
                title={'Filter'}
                ariaLabel="Close popup modal"
              />
            </Grid>
            <Grid item lg={1} xs={1}>
              {this.TypeColText.length != 0 && (
                <IconButton
                  style={{ marginLeft: 8 }}
                  title={'Clear Filter'}
                  styles={filterButton}
                  iconProps={clearFilter}
                  onClick={() => {
                    this.TypeColText = [];
                    this.setState({ TypeColKey: 0 });
                    this.columnFilter1();
                  }}
                />
              )}
            </Grid>
          </Grid>
        ),
        selector: (row: any) => <span title={row.imageType}>{row.imageType}</span>,
        width: '150px',
        isVisible: true,
      },
      {
        key: 'dateReceived',
        name: <div className={columnHeader}>{`${this.props.t('COL_DATE_RECEIVED')}`}</div>,
        selector: (row: any) => {
          if (row.receivedDate != null && row.receivedDate != '') {
            const formattedDate = new Date(row.receivedDate).toLocaleDateString('en-GB');
            return <span title={`${formattedDate}`}>{formattedDate}</span>;
          }
          return <span></span>;
        },
        isVisible: true,
        width: '130px',
      },
      {
        key: 'image',
        name: <div className={columnHeader}>{`${this.props.t('IMAGE')}`}</div>,
        selector: (row: any) => {
          const url = this.state.pictureData.find((e: any) => e.id === row.id)?.imageUrl;
          const value = url?.split('?');
          const ImgUrl = value == undefined || null ? '' : value[0];
          if (row.imageUrl) {
            const isValidUrl = () => {
              try {
                new URL(row.imageUrl);
                return true;
              } catch (e) {
                return false;
              }
            };
            return (
              <div>
                <a href={isValidUrl() && row.imageUrl} target="_blank" rel="noopener noreferrer">
                  <Icon iconName="Link" title={t('Open Link')} className="linkIcon" />
                </a>
                {/* <Icon
                  iconName="PhotoCollection"
                  title={t('IMAGE')}
                  className="iconStyle"
                  onClick={(e: any) => {
                    const link = document.createElement('a');
                    link.href = row.imageUrl;
                    link.target = '_blank';
                    link.click();
                    // window.open(ImgUrl, '_blank');
                  }}
                /> */}
              </div>
            );
          } else {
            return null;
          }
        },
        isVisible: true,
      },
      // {
      //   key: 'sampleStatus',
      //   name: <div className={columnHeader}>{`${this.props.t('COL_SAMPLE_STATUS')}`}</div>,
      //   selector: (row: any) => {
      //     if (row) {
      //       if (row.sampleStatus === 'Approved') {
      //         return (
      //           <span className={`customer-badge status-Approved`}>
      //             <span title={`${'Approved'}`}>{'Approved'}</span>
      //           </span>
      //         );
      //       } else {
      //         if (row.sampleStatus === 'Rejected') {
      //           return (
      //             <span className={`customer-badge status-Rejected`}>
      //               {'Rejected'}
      //               <span style={{ cursor: 'pointer', marginLeft: '5px' }}>
      //                 {row.samplestatusrejection != null ? (
      //                   <FaRegClone
      //                     onClick={() => {
      //                       this.setState({
      //                         recjected: true,
      //                         descriptionValue: row.samplestatusrejection,
      //                       });
      //                     }}
      //                   />
      //                 ) : (
      //                   ''
      //                 )}
      //               </span>
      //             </span>
      //           );
      //         }
      //       }
      //     }
      //   },
      //   isVisible: true,
      // },
      // {
      //   key: 'handoverDate',
      //   name: <div className={columnHeader}>{`${this.props.t('COL_HANDOVER_DATE')}`}</div>,
      //   selector: (row: any) => {
      //     if (row.sampleStatus === 'Rejected') {
      //       return null;
      //     } else {
      //       const formattedDate =
      //         row.statusDate != null && row.statusDate !== '0001-01-01T00:00:00'
      //           ? new Date(row.statusDate).toLocaleDateString('en-GB')
      //           : null;
      //       return <span title={formattedDate ?? ''}>{formattedDate}</span>;
      //     }
      //   },
      //   isVisible: true,
      //   width: '120px',
      // },
      // {
      //   key: 'handoverToBusinessStatus',
      //   name: <div className={columnHeader}>{`${this.props.t('COL_HANDOVER_STATUS')}`}</div>,
      //   selector: (row: any) => {
      //     if (this.state.filteredRecord?.length !== 0) {
      //       var handoverToBusinessStatus;
      //       if (row.sampleStatus === 'Approved') {
      //         const value = this.state.handoverOptions1.find((e: any) => e.key === 1);
      //         if (value) {
      //           handoverToBusinessStatus = value.text;
      //         }
      //         return <span title={`${handoverToBusinessStatus}`}>{handoverToBusinessStatus}</span>;
      //       } else if (row.sampleStatus === 'Rejected') {
      //         return null;
      //       } else {
      //         const value = this.state.handoverOptions1.find((e: any) => e.key === 2);
      //         if (value) {
      //           handoverToBusinessStatus = value.text;
      //         }
      //         return <span title={`${handoverToBusinessStatus}`}>{handoverToBusinessStatus}</span>;
      //       }
      //     }
      //   },
      //   isVisible: true,
      //   width: '130px',
      // },
      // {
      //   key: 'status',
      //   name: (
      //     <div className={columnHeader}>{`${this.props.t('COL_SAMPLE_RECEIVED_BY_CAM_QC')}`}</div>
      //   ),
      //   selector: (row: any) => {
      //     switch (row.camQCStatus) {
      //       case 'Pending':
      //         return (
      //           <span>
      //             <span title={`${'Pending'}`}>{'Pending'}</span>
      //           </span>
      //         );
      //       case 'Yes':
      //         return (
      //           <span className={`customer-badge status-Approved`}>
      //             <span title={`${'Received'}`}>{'Received'}</span>
      //           </span>
      //         );
      //       default:
      //         return <span></span>;
      //     }
      //   },
      //   isVisible: true,
      //   width: '130px',
      // },
    ]; //?.filter((item) => item.isVisible);

    const handleColumnReorder = (newColumns: any) => {
      var _column: any = [..._columns];
      if (newColumns == this.state.currentColumns) {
        this.setState({ currentColumns: _column });
      } else {
        this.setState({ currentColumns: newColumns });
      }
    };
    //div class name = QClayout
    return (
      <div className="QClayout">
        <div className="QcModel" />
        <Grid lg={12} item container spacing={1.5} direction={'row'}>
          <div className="QCDonwload">
            <Grid item lg={0.5} xs={6}>
              <Icon
                iconName="Download"
                title={t('BTN_DOWNLOAD')}
                className="iconStyle" //"iconStyleQC"
                onClick={() => {
                  if (this.state.filteredRecord.length !== 0) {
                    this.downloadFileQC(
                      this.state.receivedfromDate,
                      this.state.receivedtoDate,
                      this.state.handOverfromDate,
                      this.state.handOvertoDate,
                      this.state.status,
                      this.state.division,
                    );
                  }
                }}
              />
            </Grid>
          </div>
          <Grid item lg={1.98} xs={5}>
            <SearchBox
              name="search"
              className="QcsearchBox"
              styles={PIMsearchBoxStyles}
              placeholder={`${t('ITEMCODE')}`}
              onChange={(e: any) => this.onSearch(e)}
              value={this.state.searchKey}
              onClear={() => this.setState({ searchKey: '' })}
            />
          </Grid>
          <Grid item lg={0.1} xs={6}></Grid>
          <Grid item lg={1.8} xs={6}>
            {/* <Dropdown
              className="column-Topdivision-option dropdown"
              placeholder={this.props.t('PLANOGRAM_TABLE_DIVISION')}
              options={this.state.deptOption}
              key={this.state.refreshKey}
              defaultSelectedKey={1}
              styles={PIMdropdownStylesDd}
              onChange={(event, item) => {
                if (item) {
                  this.setState({ dropdown: item, DropdownVal: item.text }, () => {
                    this.Filterdata();
                  });
                }
              }}
            /> */}
            <ComboBox
              multiSelect
              placeholder={this.props.t('PLANOGRAM_TABLE_DIVISION')}
              styles={PIMdropdownStylesDd}
              options={[
                { key: 'select-toggle', text: 'Select All', selected: selecteddept },
                ...(deptOption || []).map((option: any) => ({
                  key: option.key,
                  text: option.text,
                  selected: Array.isArray(this.state.selecteddept)
                    ? this.state.selecteddept.includes(option.text)
                    : false,
                })),
              ]}
              key={this.state.refreshKey}
              onChange={(e: any, selectedOption: any) => {
                if (selectedOption && selectedOption.key === 'select-toggle') {
                  const allSelected =
                    (this.state.selecteddept || []).length === (deptOption || []).length;
                  const newSelectedSeasons = allSelected
                    ? []
                    : deptOption
                        .map((option: any) => (option.text === 'Select All' ? null : option.text))
                        .filter(Boolean);
                  this.setState(
                    { selecteddept: newSelectedSeasons, divisionSelected: '', searchKey: '' },
                    // () => {
                    //   this.Filterdata();
                    // },
                  );
                } else {
                  const updatedSelectedSeasons = Array.isArray(this.state.selecteddept)
                    ? [...this.state.selecteddept]
                    : [];
                  const isSelected = Array.isArray(this.state.selecteddept)
                    ? this.state.selecteddept.includes(selectedOption.text)
                    : false;
                  if (isSelected) {
                    const index = updatedSelectedSeasons.indexOf(selectedOption.text);
                    updatedSelectedSeasons.splice(index, 1);
                  } else {
                    updatedSelectedSeasons.push(selectedOption.text);
                  }
                  this.setState(
                    {
                      selecteddept: updatedSelectedSeasons,
                      divisionSelected: '',
                      searchKey: '',
                    },
                    // () => {
                    //   this.Filterdata();
                    // },
                  );
                }
              }}
              selectedKey={this.state.selecteddept || []}
            />
          </Grid>
          <Grid item lg={1.9} xs={6}>
            <CommonDate
              onMinDateChange={(e: any) => this.handleMinDateChange(e)}
              onToDateChange={(e: any) => this.handleToDateChange(e)}
              ref={this.commonDateRefs[0]}
              label={'Received Date'}
            />
          </Grid>
          <Grid item lg={1.9} xs={6}>
            {/* <CommonDate
              onMinDateChange={(e: any) => this.handleMinDateChange1(e)}
              onToDateChange={(e: any) => this.handleToDateChange1(e)}
              ref={this.commonDateRefs[1]}
              label={`${t('COL_HANDOVER_DATE')}`}
            /> */}
          </Grid>
          {/* <Grid item lg={1.5} xs={6}>
            <Dropdown
              placeholder={this.props.t('STATUS')}
              options={this.state.handoverOptions1}
              onChange={(e: any, selectedOption: any) => {
                this.statuss = selectedOption.text;
                this.setState({
                  isDivision: true,
                  divisionSelected: selectedOption.text,
                  searchKey: '',
                  // selecteddept:[],
                });
              }}
              key={this.state.refresh1}
              // selectedKey={this.state.divisionSelected}
              defaultSelectedKey={this.state.divisionSelected}
              styles={PIMdropdownStyles21}
            />
          </Grid> */}
          <Grid item lg={2.75} xs={6}></Grid>
          <Grid item lg={0.5} xs={2}>
            <Icon
              iconName="Search"
              title={this.props.t('BTN_SEARCH')}
              className="iconStyle"
              onClick={() => {
                if (
                  this.state.toDate != null ||
                  this.state.toDate1 != null ||
                  this.state.minDate != null ||
                  this.state.minDate1 != null ||
                  this.state.selecteddept.length != 0 ||
                  this.state.divisionSelected != ''
                ) {
                  this.onFilterClick();
                }
                // if (
                //   !(
                //     (this.state.minDate == undefined || this.state.toDate == undefined) &&
                //     (this.state.minDate1 == undefined || this.state.toDate1 == undefined) &&
                //     (this.state.divisionSelected == null || this.state.divisionSelected == '')
                //   )
                // ) {
                //   this.onFilterClick();
                // }
                // else {
                //   this.notify.showErrorNotify(
                //     this.props.t('PLEASE_SELECT_RECEIVEDDATE,_HANDOVERDATE_OR_STATUS'),
                //   );
                // }
              }}
            />
          </Grid>
          <Grid item lg={0.5} xs={1}>
            <Icon
              iconName="Clear"
              title={t('BTN_CLEAR')}
              className="iconStyle"
              onClick={() => {
                if (
                  this.state.toDate != null ||
                  this.state.toDate1 != null ||
                  this.state.minDate != null ||
                  this.state.minDate1 != null ||
                  this.state.selecteddept.length != 0 ||
                  this.state.searchKey != '' ||
                  this.state.divisionSelected != ''
                ) {
                  this.setState({
                    fromDate: null,
                    toDate: null,
                    searchKey: '',
                    downloadToDate: null,
                    downloadFromDate: null,
                  });
                  this.refresh();
                  this.resetDates();
                }
              }}
            />
          </Grid>
          <Grid item lg={0.5} xs={2}>
            <Icon
              iconName="Refresh"
              title={this.props.t('BTN_REFRESH')}
              className="iconStyle"
              onClick={() => this.refresh()}
            />
          </Grid>
        </Grid>
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
        <Modal
          isOpen={this.state.approveVisible}
          containerClassName={PIMcontentStyles.confirmContaineruser1}
          isBlocking={false}
          onDismiss={() =>
            this.setState({
              approveVisible: false,
              row: [],
            })
          }
        >
          <div className={PIMcontentStyles.confirmHeaderUser1}>
          <IconButton
            styles={UploadStyles}
            iconProps={cancelIcon}
            ariaLabel="Close popup modal"
            onClick={() => this.setState({ approveVisible: false, row: [] })}
          />
          </div>
          <div className={PIMcontentStyles.confirmbody4}>
            <Text variant="xLarge" className="apptext">{`${this.props.t(
              'PHOTOSHOOT_TRACKING_MODEL_APPROVE',
            )}`}</Text>
          </div>
          <div className={PIMcontentStyles.footer}>
            <Grid lg={12} item container spacing={2}>
              <Grid item lg={1.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonConfirm}
                  text={`${this.props.t('BTN_CONFIRM')}`}
                  onClick={() => this.onRejectorAprove()}
                />
              </Grid>
              <Grid item lg={0.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonCancel}
                  text={`${this.props.t('BTN_CANCEL')}`}
                  onClick={() => {
                    this.setState({ approveVisible: false, row: [] });
                  }}
                />
              </Grid>
              <Grid item lg={2} xs={12} />
            </Grid>
          </div>
        </Modal>
        <Modal
          isOpen={this.state.recjectVisible}
          containerClassName={PIMcontentStyles.container4}
          isBlocking={false}
          onDismiss={() =>
            this.setState({
              recjectVisible: false,
              row: [],
            })
          }
        >
          <IconButton
            styles={iconButtonStyles}
            iconProps={{ iconName: 'Cancel' }}
            ariaLabel="Close popup modal"
            onClick={() => this.setState({ recjectVisible: false, row: [] })}
          />
          <div className={PIMcontentStyles.confirmbody}>
            <Text variant="xLarge" className="apptext">
              {`${this.props.t('PLANOGRAM_TABLE_MODEL_REJECT')}`}
            </Text>
          </div>
          <div className={PIMcontentStyles.body}>
            <form noValidate autoComplete="off">
              <TextField
                required
                name="otherComment"
                className="textField rejectField"
                label={this.props.t('Reject Comment')}
                onChange={(event: any) =>
                  this.setState({
                    otherSplitComment: event.target.value.trim().replace(/\s{2,}/g, ' '),
                  })
                }
                multiline
                resizable={false}
                maxLength={500}
                autoAdjustHeight={true}
              />
            </form>
          </div>

          <div className={PIMcontentStyles.footer}>
            <Grid lg={12} item container spacing={2}>
              <Grid item lg={1.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonConfirm}
                  text={`${this.props.t('BTN_CONFIRM')}`}
                  onClick={() => this.onRejectorAprove()}
                  disabled={this.state.otherSplitComment.trim() == ''}
                />
              </Grid>
              <Grid item lg={0.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonCancel}
                  text={`${this.props.t('BTN_CANCEL')}`}
                  onClick={() => {
                    this.setState({
                      recjectVisible: false,
                      // selectedRejectComment: null,
                      // otherComment: false,
                      // row: [],
                    });
                  }}
                />
              </Grid>
              <Grid item lg={2} xs={12} />
            </Grid>
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
        <Modal
          isOpen={this.state.imgVisible}
          containerClassName={PIMcontentStyles.imageContainer}
          isBlocking={true}
          onDismiss={() =>
            this.setState({
              imgVisible: false,
            })
          }
        >
          <div className={PIMcontentStyles.header}>
            <span className="apptext1">{t('IMAGE_UPLOAD')}</span>
            <IconButton
              styles={iconButtonStyles}
              iconProps={cancelIcon}
              ariaLabel="Close popup modal"
              onClick={() => this.setState({ imgVisible: false })}
            />
          </div>
          <div className={PIMcontentStyles.imagebody}>
            {this.state.imageLoading == false ? (
              <Image
                src={this.state.imageURL}
                alt="picture"
                style={{ maxWidth: '500px', maxHeight: '400px', width: '100%', height: '100%' }}
              />
            ) : (
              <h3>{`${t('NO_IMAGE_UPLOADED')}`}</h3>
            )}
          </div>
        </Modal>
        <div className="QCTable dataTableFixedFooter">
          <DataTable
            columns={_columns}
            // className="QCTable"
            data={this.state.filteredRecord}
            pagination={true}
            selectableRowsHighlight={this.state.selectableRowsHighlight == false ? false : true}
            clearSelectedRows={this.state.noSelection}
            selectableRows={this.state.selectedRowData}
            selectableRowsVisibleOnly={this.state.selectedRowData}
            onSelectedRowsChange={this.rowSelection}
            onColumnOrderChange={handleColumnReorder}
            highlightOnHover
            fixedHeader
            striped
            fixedHeaderScrollHeight="62.6vh"
            paginationComponentOptions={{ rowsPerPageText: `${this.props.t('ROWS_PER_PAGE')}` }}
            paginationPerPage={this.Defaultperpage()}
            paginationRowsPerPageOptions={this.perpage()}
            noDataComponent={
              <div className="noDataWidth">
                {<DataTable columns={_columns} data={[{ '': '' }]} />}
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
    );
  }
}

const ComponentTranslated = withTranslation()(ReceivedProducts);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
