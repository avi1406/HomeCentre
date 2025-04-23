import React, { Suspense } from 'react';
import 'react-phone-input-2/lib/style.css';
import { SearchBox } from '@fluentui/react/lib/SearchBox';
import DataTable from 'react-data-table-component';
import { DefaultButton, IconButton } from '@fluentui/react/lib/Button';
import { Text } from '@fluentui/react/lib/Text';
import { ComboBox, DatePicker, Dropdown, IColumn, Modal, Stack } from '@fluentui/react';
import AuthManagerService from '@/services/AuthManagerService';
import ApiManager from '@/services/ApiManager';
import 'office-ui-fabric-react/dist/css/fabric.css';
import Util from '@/services/Util';
import {
  CancelStyles,
  PlanoCancelStyles,
  PlanoDisStyles,
  cancelIcon,
  clearFilter,
  columnFilter,
  filterButton,
  filterIcon,
  iconButtonStyles,
  rejectComment,
} from '@/common/properties';
import { withTranslation } from 'react-i18next';
import { Icon, TextField } from 'office-ui-fabric-react';
import { Grid } from '@mui/material';
import jwt_decode from 'jwt-decode';
import { FaRegClone } from 'react-icons/fa';
import { TasklevelSequence, UidSearchModel } from '@/services/ApiClient';
import NotificationManager from '@/services/NotificationManager';
import {
  clearBtnStyle,
  columnHeader,
  columnHeaderPlano,
  PIMButtons,
  PIMcontentStyles,
  PIMdropdownStyles2,
  PIMHearderText,
  PIMsearchBoxStyles,
} from '../PimStyles';
import { baseUrl } from '@/services/Constants';
import CommonDate, { CommonDateRef } from '../../pages/commonFunctions';

class PlanogramCommon extends React.Component<any, any> {
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
  tenantId = this.authManager.getTenantId();
  activateRecord: any;
  isEdit: boolean | undefined;
  selectedRowData: null | undefined;
  roleColumn: IColumn[] | undefined;
  stausofRecords = true;
  isGetall: boolean | undefined;
  division: any = [];
  actionL1: any = [];
  actionL2: any = [];

  commonDateRef = React.createRef<CommonDateRef>();

  constructor(props: any) {
    super(props);

    this.state = {
      currentRecord: [],
      searchKey: '',
      selectedRowData: [],
      noSelection: false,
      ApprovalEnable: true,
      descriptionValue: '',
      row: [],
      rejectComment: '',
      dropDownSelectedRecord: [],
      secondLevelComments: '',
      searchResult: [],
      filteredRecord: [],
      A_D_Visible: false,
      A_D_Visible1: false,
      rejectCommentList: [],
      otherComment: false,
      minDate: undefined,
      toDate: undefined,
      searchDivision: [],
      onClickMinDate: undefined,
      onClickToDate: undefined,
      onClickSearchDivision: null,
      divisionOptionlst: [],
      Approvaloption: [],
      SecoundApprovalOption: [],
      isRecord: false,
      onHover: false,
      selectionCount: 0,
      deptOption: [],
      isACL2: false,
      isACL1: false,
      isDivision: false,
      divisionSelected: 0,
      actionL1Selected: 0,
      actionL2Selected: 0,
      otherSplitComment: '',
      datePickerVisible: false,
      minDate_Day: '',
      minDate_Month: '',
      minDate_Year: '',
      toDate_Day: '',
      toDate_Month: '',
      toDate_Year: '',
      exportvisible: false,
    };
  }

  async componentDidMount(): Promise<void> {
    this.setState({ isRecord: true });
    const rejectComment = (await this.apiClient.getRejectReason()).result;
    var comment: any = [];
    rejectComment?.forEach((element) => {
      comment.push({
        key: element.name,
        text: element.name,
      });
    });
    this.setState({ rejectCommentList: comment });
    this.apiClient.getPlanogramRecordList().then((r) => {
      var departmentList: any = [];
      var ApprovalOption: any = [];
      var SecondApprovalOption: any = [];
      r.result?.forEach((element: any) => {
        if (departmentList.filter((e: any) => e.key == element.division).length == 0) {
          departmentList.push({
            key: element.division,
            text: element.division,
          });
        }
      });
      r.result?.forEach((element: any) => {
        if (
          ApprovalOption.filter((e: any) => e.key == element.planoOrFamilyNameCreationStatusL1)
            .length == 0
        ) {
          ApprovalOption.push({
            key: element.planoOrFamilyNameCreationStatusL1,
            text: element.planoOrFamilyNameCreationStatusL1,
          });
        }
      });
      r.result?.forEach((element: any) => {
        if (
          SecondApprovalOption.filter(
            (e: any) => e.key == element.planoOrFamilyNameCreationStatusL2,
          ).length == 0
        ) {
          SecondApprovalOption.push({
            key: element.planoOrFamilyNameCreationStatusL2,
            text: element.planoOrFamilyNameCreationStatusL2,
          });
        }
      });
      this.setState({
        divisionOptionlst: departmentList,
        Approvaloption: ApprovalOption,
        SecoundApprovalOption: SecondApprovalOption,
      });
    });
    var departmentList = (await this.apiClient.getDivision())?.result;
    var departmentOption: { key: number | undefined; text: string | undefined }[] = [];
    var deptOpt: { key: number | undefined; text: string | undefined }[] = [];
    departmentList?.forEach((element) => {
      departmentOption.push({ key: element.id, text: element.name });
      deptOpt.push({ key: element.id, text: element.name });
    });
    const token = sessionStorage.getItem('token');
    const decode: any = jwt_decode(token || '');
    const configValue: any = await this.apiClient.getAppConfig();
    const roleId: any = decode.RoleId.split(',');
    const appConfiguration = JSON.parse(
      configValue.result.find((e: any) => e.name == 'ApproversList').jsonValue,
    );
    this.setState({
      departmentOption: departmentOption,
      deptOption: deptOpt,
    });
    this.refresh();
  }
  ClearDates() {
    if (this.commonDateRef.current) {
      this.commonDateRef.current.resetDates();
    }
  }
  async refresh() {
    this.setState({ isRecord: true, searchKey: '' });
    const roles: any = this.roleId;
    var body = new UidSearchModel();
    try {
      this.setState({ searchKey: '' });
      const response: any = (await this.apiClient.uidSearchByCriteria(body)).result;
      const responeWithIndex = response.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      }));

      this.setState({
        ApprovalEnable: true,
        currentRecord: responeWithIndex,
        dropDownSelectedRecord: responeWithIndex,
        filteredRecord: responeWithIndex,
        A_D_Visible1: false,
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
    this.division = [];
    this.actionL1 = [];
    this.actionL2 = [];
    this.setState({
      noSelection: !this.state.noSelection,
      selectedRowData: [],
      selectionCount: 0,
      ApprovalEnable: true,
      isRecord: false,
      minDate: undefined,
      onClickToDate: undefined,
      onClickMinDate: undefined,
      actionL1Selected: 0,
      actionL2Selected: 0,
      searchDivision: [],
      toDate: undefined,
      dropDownSelectedRecord: this.state.currentRecord,
      filteredRecord: this.state.currentRecord,
      exportvisible: this.state.currentRecord.length !== 0 ? true : false,
      divisionSelected: 0,
      Division: '',
      ActionL2: '',
      ActionL1: '',
      isACL2: false,
      isDivision: false,
      isACL1: false,
      otherSplitComment: '',
      division: [],
      actionL1: [],
      actionL2: [],
      minDate_Day: '',
      minDate_Month: '',
      minDate_Year: '',
      toDate_Day: '',
      toDate_Month: '',
      toDate_Year: '',
    });
    if (this.commonDateRef.current) {
      this.commonDateRef.current.resetDates();
    }
  }

  async onReject() {
    try {
      this.setState({ recjectVisible: false, selectedRejectComment: null, otherComment: false });
      var { selectedRejectComment, otherSplitComment } = this.state;
      var ids: TasklevelSequence[] = [];
      var tasklevelSeq = new TasklevelSequence();
      tasklevelSeq.taskStepInstanceId = this.state.row.taskStepInstanceId;
      tasklevelSeq.recordId = this.state.row.recordId;
      tasklevelSeq.userAction = 'Reject';
      tasklevelSeq.userComments = selectedRejectComment;
      tasklevelSeq.rejectDescription = selectedRejectComment == 'other' ? otherSplitComment : '';
      ids.push(tasklevelSeq);
      const response = await this.apiClient.updateUserAction(ids);
      this.setState({ selectedRejectComment: null, otherComment: false });
      if (response.hasError) {
        this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN') || response.message);
      } else {
        this.notify.showSuccessNotify(this.props.t('REJECTED_SUCESSFULLY'));
      }
      this.refresh();
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
  }

  onSearch(e: any) {
    var newValue = e?.target.value;
    newValue = newValue == undefined ? '' : newValue;
    var { dropDownSelectedRecord } = this.state;
    var searchResult = dropDownSelectedRecord.filter((element: any) => {
      return element?.planoOrFamilyName
        ?.toLowerCase()
        .includes(newValue.trim().split(/ +/).join(' ').toLowerCase());
    });
    this.setState({ filteredRecord: searchResult, searchKey: newValue });
    this.setState({
      filteredRecord: searchResult.map((item: any, index: any) => ({ index: index + 1, ...item })),
      exportvisible: searchResult.length !== 0 ? true : false,
    });
  }

  columnFilter = async () => {
    var data: any = [];
    var secondaryFilter: any = [];
    var finalData: any = [];
    if (this.division.length != 0) {
      data = this.state.currentRecord.filter((element: any) => {
        return element.division == this.division;
      });
    } else {
      data = this.state.currentRecord;
    }
    if (this.actionL1.length != 0) {
      secondaryFilter = data.filter((element: any) => {
        return element.planoOrFamilyNameCreationStatusL1 == this.actionL1;
      });
    } else {
      secondaryFilter = data;
    }
    if (this.actionL2.length != 0) {
      finalData = secondaryFilter.filter((element: any) => {
        return element.planoOrFamilyNameCreationStatusL2 == this.actionL2;
      });
    } else {
      finalData = secondaryFilter;
    }
    this.setState({
      dropDownSelectedRecord: finalData,
      filteredRecord: finalData,
      exportvisible: finalData.length !== 0 ? true : false,
    });
  };

  async onFilterClick() {
    var { toDate, minDate, searchDivision, deptOption } = this.state;
    const filteredSearchDivision = deptOption.filter((item: any) =>
      searchDivision.includes(item.text),
    );
    var body = new UidSearchModel();
    if (searchDivision.length !== 0) {
      const departmentIdArray = filteredSearchDivision.map((item: any) => item.key).join(',');
      body.departmentId = departmentIdArray;
    }
    if (minDate != undefined) {
      const dateParts = new Date(minDate).toLocaleDateString('en-CA').split('/');
      body.fromDate = dateParts[0];
    }
    if (toDate != undefined) {
      const dateParts = new Date(toDate).toLocaleDateString('en-CA').split('/');
      body.toDate = dateParts[0];
    }

    const response = (await this.apiClient.uidSearchByCriteria(body)).result;
    const responeWithIndex = response?.map((item: any, index: any) => ({
      index: index + 1,
      ...item,
    }));
    this.setState({
      searchKey: '',
      isDivision: false,
      divisionSelected: 0,
      isACL1: false,
      actionL1Selected: 0,
      isACL2: false,
      actionL2Selected: 0,
      onClickMinDate: minDate,
      onClickToDate: toDate,
      onClickSearchDivision: searchDivision,
      currentRecord: responeWithIndex,
      dropDownSelectedRecord: responeWithIndex,
      filteredRecord: responeWithIndex,
      exportvisible: responeWithIndex?.length !== 0 ? true : false,
    });
  }
  handleMinDateChange = (date: any) => {
    this.setState({ minDate: date });
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
    this.setState({ toDate: date });
  };
  async Columnaction(row: any) {
    let action1 = 'Rejected';
    let action2 = 'Rejected';
    var planofamname = row?.planoOrFamilyName.replace(/^\s+|\s+$/gm, '');
    if (action1 && action2) {
      const res = await this.apiClient.updateRejectedName([planofamname]);
      // const response = res?.result;
      // this.notify.showSuccessNotify(response.message);
      this.refresh();
    }
  }

  // async UidExportExcel() {
  //   var { onClickToDate, onClickMinDate, onClickSearchDivision } = this.state;
  //   var body: UidSearchModel = new UidSearchModel();
  //   if (onClickSearchDivision != undefined) body.departmentId = onClickSearchDivision?.key;

  //   if (onClickMinDate != undefined) {
  //     const dateParts = new Date(onClickMinDate).toLocaleDateString('en-CA').split('/');
  //     body.fromDate = dateParts[0];
  //   }

  //   if (onClickToDate != undefined) {
  //     const dateParts = new Date(onClickToDate).toLocaleDateString('en-CA').split('/');
  //     body.toDate = dateParts[0];
  //   }
  //   fetch(baseUrl + '/api/WorkFlow/UIDExportExcel', {
  //     method: 'POST',
  //     body: JSON.stringify(body),
  //     headers: {
  //       'Content-Type': 'application/json;',
  //     },
  //   })
  //     .then((response: any) => {
  //       if (response.status === 200) {
  //         return response.blob();
  //       } else {
  //         return;
  //       }
  //     })
  //     .then((body: any) => {
  //       var blob = [body];
  //       var a = document.createElement('a');
  //       a.href = window.URL.createObjectURL(
  //         new Blob(blob, {
  //           type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  //         }),
  //       );
  //       a.download = this.props.t('EXPORT_EXCEL_FILE_NAME');
  //       a.click();
  //     });
  //   this.notify.showSuccessNotify(this.props.t('DOWNLOAD_STARTED'));
  async UidExportExcel() {
    document.body.classList.add('loading-indicator');
    this.notify.ImageHideNotify(this.props.t('DOWNLOAD_STARTED'));
    var { onClickToDate, onClickMinDate, onClickSearchDivision, deptOption, searchDivision } =
      this.state;
    var body: UidSearchModel = new UidSearchModel();
    // if (onClickSearchDivision != undefined) body.departmentId = onClickSearchDivision?.key;

    const filteredSearchDivision = deptOption.filter((item: any) =>
      searchDivision.includes(item.text),
    );
    if (searchDivision.length !== 0) {
      const departmentIdArray = filteredSearchDivision.map((item: any) => item.key).join(',');
      body.departmentId = departmentIdArray;
    }
    if (onClickMinDate != undefined) {
      const dateParts = new Date(onClickMinDate).toLocaleDateString('en-CA').split('/');
      body.fromDate = dateParts[0];
    }

    if (onClickToDate != undefined) {
      const dateParts = new Date(onClickToDate).toLocaleDateString('en-CA').split('/');
      body.toDate = dateParts[0];
    }
    fetch(baseUrl + '/api/WorkFlow/UIDExportExcel', {
      method: 'POST',
      body: JSON.stringify(body),
      headers: {
        'Content-Type': 'application/json;',
      },
    })
      .then((response: any) => {
        if (response.status === 200) {
          return response.blob();
        } else {
          return;
        }
      })
      .then((body: any) => {
        this.notify.showSuccessNotify(this.props.t('DOWNLOAD_COMPLETED'));
        var blob = [body];
        var a = document.createElement('a');
        a.href = window.URL.createObjectURL(
          new Blob(blob, {
            type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
          }),
        );
        a.download = this.props.t('EXPORT_EXCEL_FILE_NAME');
        a.click();
        document.body.classList.remove('loading-indicator');
      });
  }
  render() {
    const { deptOption } = this.state;
    const selecteddept =
      deptOption && deptOption.length
        ? this.state.searchDivision.length === deptOption.length
        : false;
    var _columns: any = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{this.props.t('S_NO')}</div>,
        selector: (row: any) => row.index,
        isVisible: true,
        // sortable: true,
        // sortFunction: (a: any, b: any) => a.index - b.index,
        width: '68px',
      },
      {
        key: 'PlanoOrFamilyname',
        name: (
          <div className={columnHeader}>{this.props.t('PLANOGRAM_TABLE_PLANO/FAMILY_NAME')}</div>
        ),
        selector: (row: any) => <span title={row.planoOrFamilyName}>{row.planoOrFamilyName}</span>,
        isVisible: true,
        // sortable: true,
        // sortFunction: (a: any, b: any) =>
        //   a.planoOrFamilyName?.toString()?.localeCompare(b.planoOrFamilyName?.toString()),
        width: '138px',
      },
      {
        key: 'PlanoOrFamilyCode',
        name: (
          <div className={columnHeader}>{this.props.t('PLANOGRAM_TABLE_PLANO/FAMILY_CODE')}</div>
        ),
        selector: (row: any) => <span title={row.planoOrFamilyCode}>{row.planoOrFamilyCode}</span>,
        isVisible: true,
        width: '138px',
        position: 'fixed',
      },
      {
        key: 'Division',
        name: (
          <Grid container direction={'row'}>
            <Grid item lg={8} xs={9}>
              <div className={columnHeader}>{this.props.t('PLANOGRAM_TABLE_DIVISION')}</div>
            </Grid>
            <Grid item lg={2} xs={3}>
              <ComboBox
                className="dropdown"
                options={this.state.departmentOption}
                onChange={(e: any, selectedOption: any) => {
                  this.division = selectedOption.text;
                  this.setState({
                    isDivision: true,
                    divisionSelected: selectedOption.key,
                    searchKey: '',
                  });
                  this.columnFilter();
                }}
                selectedKey={this.state.divisionSelected}
                styles={columnFilter}
                style={{ marginLeft: -25 }}
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
                  iconProps={clearFilter}
                  onClick={() => {
                    this.division = [];
                    this.setState({ isDivision: false, divisionSelected: 0 });
                    this.columnFilter();
                  }}
                />
              )}
            </Grid>
          </Grid>
        ),
        position: 'fixed',
        selector: (row: any) => <span title={row.division}>{row.division}</span>,
        isVisible: true,
        width: '130px',
      },
      {
        key: 'Type',
        name: <div className={columnHeader}>{this.props.t('PLANOGRAM_TYPE')}</div>,
        isVisible: true,
        // selector: (row: any) => row.requestType == 'Planogram' ? this.props.t('PLANOGRAM_NAME') : row.requestType,
        selector: (row: any) => (
          <span
            title={
              row.requestType === 'Planogram' ? this.props.t('PLANOGRAM_NAME') : row.requestType
            }
          >
            {row.requestType === 'Planogram' ? this.props.t('PLANOGRAM_NAME') : row.requestType}
          </span>
        ),
        width: '135px',
      },
      {
        key: 'Description',
        name: <div className={columnHeader}>{this.props.t('PLANOGRAM_TABLE_DESCRIPTION')} </div>,
        isVisible: true,
        width: '120px',
        selector: (row: any) => (
          <>
            <span>
              <span
                title={row.description}
                onClick={() => {
                  this.setState({ descriptionValue: row.description, discriptionVisible: true });
                }}
              >
                {row.description}
              </span>
            </span>
          </>
        ),
      },
      {
        key: 'ActionL1',
        name: (
          <Grid container direction={'row'}>
            <Grid item lg={10} xs={9}>
              <div className={columnHeaderPlano}>{this.props.t('PLANOGRAM_TABLE_ACTION_L1')}</div>
            </Grid>
            <Grid item lg={2} xs={3}>
              <ComboBox
                className="dropdown"
                options={this.state.Approvaloption}
                onChange={(e: any, selectedOption: any) => {
                  this.actionL1 = selectedOption.text;
                  this.setState({
                    isACL1: true,
                    actionL1Selected: selectedOption.key,
                    searchKey: '',
                  });
                  this.columnFilter();
                }}
                styles={columnFilter}
                selectedKey={this.state.actionL1Selected}
                style={{ marginTop: 15, marginLeft: '-15px' }}
              />
              <IconButton
                style={{ marginLeft: 15 }}
                styles={filterButton}
                iconProps={filterIcon}
                title={'Filter'}
                ariaLabel="Close popup modal"
              />
            </Grid>
            <Grid item lg={1} xs={1}>
              {this.state.isACL1 && (
                <IconButton
                  title={'Clear Filter'}
                  styles={filterButton}
                  iconProps={clearFilter}
                  onClick={() => {
                    this.actionL1 = [];
                    this.setState({ isACL1: false, actionL1Selected: 0 });
                    this.columnFilter();
                  }}
                  style={{ marginLeft: 33 }}
                />
              )}
            </Grid>
          </Grid>
        ),
        width: '160px',
        position: 'fixed',
        isVisible: true,
        selector: (row: any) => {
          return (
            <span
              className={`customer-badge status-${row.planoOrFamilyNameCreationStatusL1}`}
              title={row.planoOrFamilyNameCreationStatusL1}
            >
              {row.planoOrFamilyNameCreationStatusL1}
              <span style={{ cursor: 'pointer' }}>
                {row.planoOrFamilyNameCreationStatusL1 == 'Rejected' ? (
                  <FaRegClone
                    onClick={() => {
                      this.setState({
                        recjected: true,
                        descriptionValue: row.l1RejectDescription
                          ? row.l1RejectDescription
                          : row.firstLevelComments,
                      });
                    }}
                  />
                ) : (
                  ''
                )}
              </span>
            </span>
          );
        },
      },
      {
        key: 'ActionL2',
        name: (
          <Grid container direction={'row'}>
            <Grid item lg={10} xs={9}>
              <div className={columnHeaderPlano}>{this.props.t('PLANOGRAM_TABLE_ACTION_L2')}</div>
            </Grid>
            <Grid item lg={2} xs={3}>
              <ComboBox
                className="dropdown"
                options={this.state.SecoundApprovalOption}
                onChange={(e: any, selectedOption: any) => {
                  this.actionL2 = selectedOption.text;
                  this.setState({
                    isACL2: true,
                    actionL2Selected: selectedOption.key,
                    searchKey: '',
                  });
                  this.columnFilter();
                }}
                selectedKey={this.state.actionL2Selected}
                styles={columnFilter}
                style={{ marginTop: 15, marginLeft: '-15px' }}
                title={'Filter'}
              />
              <IconButton
                style={{ marginLeft: 15 }}
                styles={filterButton}
                title={'Filter'}
                iconProps={filterIcon}
                ariaLabel="Close popup modal"
              />
            </Grid>
            <Grid item lg={1} xs={1}>
              {this.state.isACL2 && (
                <IconButton
                  title={'Clear Filter'}
                  styles={filterButton}
                  iconProps={clearFilter}
                  onClick={() => {
                    this.actionL2 = [];
                    this.setState({ isACL2: false, actionL2Selected: 0 });
                    this.columnFilter();
                  }}
                  style={{ marginLeft: 33 }}
                />
              )}
            </Grid>
          </Grid>
        ),
        minWidth: '160px',
        position: 'fixed',
        isVisible: true,
        selector: (row: any) => {
          return (
            <span
              className={`customer-badge status-${row.planoOrFamilyNameCreationStatusL2}`}
              title={row.planoOrFamilyNameCreationStatusL2}
            >
              {row.planoOrFamilyNameCreationStatusL2}
              <span style={{ cursor: 'pointer' }}>
                {row.planoOrFamilyNameCreationStatusL2 == 'Rejected' ? (
                  <FaRegClone
                    onClick={() => {
                      this.setState({
                        recjected: true,
                        descriptionValue: row.l2RejectDescription
                          ? row.l2RejectDescription
                          : row.secondLevelComments,
                      });
                    }}
                  />
                ) : (
                  ''
                )}
              </span>
            </span>
          );
        },
      },
      {
        key: 'Plano/FamilyCodeStatus',
        name: <div className={columnHeader}>{this.props.t('PLANO_FAMILY_CODE_STATUS')}</div>,
        selector: (row: any) => (
          <span title={row.planoOrFamilyCodeStatus}>{row.planoOrFamilyCodeStatus}</span>
        ),
        isVisible: true,
        width: '140px',
      },
      {
        key: 'ApproverL1',
        name: <div className={columnHeader}>{this.props.t('PLANOGRAM_TABLE_APPROVER_L1')}</div>,
        width: '180px',
        isVisible: true,
        selector: (row: any) => <span title={row.actionedbyL1}>{row.actionedbyL1}</span>,
      },

      {
        key: 'DateofActionbyL1',
        name: (
          <div className={columnHeader}>{this.props.t('PLANOGRAM_TABLE_DATE_OF_ACTION_BY_L1')}</div>
        ),
        isVisible: true,
        selector: (row: any) => (
          <p title={row.l1ActionDate != null && row.l1ActionDate != '' && row.l1ActionDate}>
            {row.l1ActionDate != null && row.l1ActionDate != '' && row.l1ActionDate}
          </p>
        ),
        width: '110px',
      },
      {
        key: 'ApproverL2',
        name: <div className={columnHeader}> {this.props.t('PLANOGRAM_TABLEE_APPROVAL_L2')}</div>,
        isVisible: true,
        width: '180px',
        selector: (row: any) => <span title={row.actionedbyL2}>{row.actionedbyL2}</span>,
      },
      {
        key: 'DateofActionbyL2',
        name: (
          <div className={columnHeader}>{this.props.t('PLANOGRAM_TABLE_DATE_OF_ACTION_BY_L2')}</div>
        ),
        isVisible: true,
        selector: (row: any) => (
          <span title={row.l2ActionDate != null && row.l2ActionDate != '' && row.l2ActionDate}>
            <p>{row.l2ActionDate != null && row.l2ActionDate != '' && row.l2ActionDate}</p>
          </span>
        ),
        width: '110px',
      },
      {
        key: 'CreatedBy',
        name: <div className={columnHeader}>{this.props.t('PLANOGRAM_TABLE_CREATEBY')}</div>,
        selector: (row: any) => (
          <span title={row.planoOrFamilyCreatedBy}>{row.planoOrFamilyCreatedBy}</span>
        ),
        isVisible: true,
        width: '140px',
      },
      {
        key: 'DateCreated',
        name: <div className={columnHeader}>{this.props.t('PLANOGRAM_TABLE_DATE_CREATED')}</div>,
        isVisible: true,
        width: '140px',
        selector: (row: any) => (
          <span
            title={
              row.planoOrFamilyCreatedDate != null &&
              row.planoOrFamilyCreatedDate != '' &&
              row.planoOrFamilyCreatedDate
            }
          >
            <p>
              {row.planoOrFamilyCreatedDate != null &&
                row.planoOrFamilyCreatedDate != '' &&
                row.planoOrFamilyCreatedDate}
            </p>
          </span>
        ),
      },
      {
        key: 'CodeCreatedDate',
        name: (
          <div className={columnHeader}>{this.props.t('PLANOGRAM_TABLE_CODE_CREATED_DATE')}</div>
        ),
        isVisible: true,
        width: '140px',
        selector: (row: any) => (
          <span
            title={
              row.planoOrFamilyCodeUpdatedDate != null &&
              row.planoOrFamilyCodeUpdatedDate != '' &&
              row.planoOrFamilyCodeUpdatedDate
            }
          >
            <p>
              {row.planoOrFamilyCodeUpdatedDate != null &&
                row.planoOrFamilyCodeUpdatedDate != '' &&
                row.planoOrFamilyCodeUpdatedDate}
            </p>
          </span>
        ),
      },
      {
        key: 'action',
        name: <div className={columnHeader}>{this.props.t('COL_ACTION')}</div>,
        isVisible: this.roleId?.includes(1) ? true : false,
        width: '110px',
        selector: (row: any) => (
          <p>
            {row.planoOrFamilyNameCreationStatusL1 == 'Rejected' ||
            row.planoOrFamilyNameCreationStatusL2 == 'Rejected' ? (
              <Icon
                iconName="RevToggleKey"
                className="iconStyle"
                title={this.props.t('Re-Initiate')}
                onClick={() => {
                  this.Columnaction(row);
                }}
              />
            ) : (
              ''
            )}
          </p>
        ),
      },
    ];
    _columns = _columns.filter((f: any) => f?.isVisible === true);
    const handleColumnReorder = (newColumns: any) => {
      var _column: any = [..._columns];
      if (newColumns == this.state.currentColumns) {
        this.setState({ currentColumns: _column });
      } else {
        this.setState({ currentColumns: newColumns });
      }
    };
    return (
      <div className="layout width-100" style={{ flexWrap: 'wrap' }}>
        <div className="bg-Color">
          <>
            {/* <Text block className="text">{`${this.props.t('NAV_PLANOGRAM')} / ${this.props.t(
              'NAV_PLANOGRAM_RECORD',
            )}`}</Text> */}
            <Grid
              lg={12}
              item
              container
              spacing={2}
              direction={'row'}
              style={{ marginBottom: '0.2%' }}
            >
              <Grid item lg={10.5} xs={10}>
                <Text
                  variant="xxLarge"
                  styles={PIMHearderText}
                  className="color-blue text"
                >{`${this.props.t('MENU_SCHEMATIC_RECORDS')}`}</Text>
              </Grid>
              <Grid item lg={0.9} xs={0} />
              <Grid item lg={0.5} xs={1}>
                <Icon
                  iconName="Download"
                  className="iconStyle"
                  title={this.props.t('BTN_DOWNLOAD')}
                  onClick={() => {
                    if (this.state.exportvisible) {
                      this.UidExportExcel();
                    }
                  }}
                />
              </Grid>
            </Grid>
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={2.5} xs={12}>
                <SearchBox
                  name="search"
                  className="searchBox"
                  styles={PIMsearchBoxStyles}
                  placeholder={this.props.t('PLANOGRAM_SEARCH_PLACEHOLDER')}
                  onChange={(e: any) => this.onSearch(e)}
                  value={this.state.searchKey}
                  onClear={() => this.setState({ searchKey: '' })}
                />
              </Grid>
              <Grid item lg={2.1} xs={12}>
                <CommonDate
                  onMinDateChange={(e: any) => this.handleMinDateChange(e)}
                  onToDateChange={(e: any) => this.handleToDateChange(e)}
                  label={`${this.props.t('PLANOCREATION_DATE')}`}
                  ref={this.commonDateRef}
                />
              </Grid>
              <Grid item lg={0.5} xs={12}>
                <Stack horizontal>
                  {/* <Dropdown
                    className="column-Topdivision-option dropdown"
                    placeholder={this.props.t('PLANOGRAM_TABLE_DIVISION')}
                    onChange={(e, selectedOption) =>
                      this.setState({ searchDivision: selectedOption })
                    }
                    options={this.state.deptOption}
                    defaultSelectedKey={this.state.searchDivision?.key}
                    selectedKey={
                      this.state.searchDivision != null ? this.state.searchDivision.key : null
                    }
                    styles={PIMdropdownStyles2}
                    style={{ marginTop: -8 }}
                  /> */}
                  <ComboBox
                    multiSelect
                    placeholder={this.props.t('PLANOGRAM_TABLE_DIVISION')}
                    styles={clearBtnStyle}
                    options={[
                      { key: 'select-toggle', text: 'Select All', selected: selecteddept },
                      ...(deptOption || []).map((option: any) => ({
                        key: option.key,
                        text: option.text,
                        selected: Array.isArray(this.state.searchDivision)
                          ? this.state.searchDivision.includes(option.text)
                          : false,
                      })),
                    ]}
                    key={this.state.refreshKey}
                    onChange={(e: any, selectedOption: any) => {
                      if (selectedOption && selectedOption.key === 'select-toggle') {
                        const allSelected =
                          (this.state.searchDivision || []).length === (deptOption || []).length;
                        const newSelectedSeasons = allSelected
                          ? []
                          : deptOption
                              .map((option: any) =>
                                option.text === 'Select All' ? null : option.text,
                              )
                              .filter(Boolean);
                        this.setState(
                          {
                            searchDivision: newSelectedSeasons,
                            searchKey: '',
                            divisionSelected: '',
                          },
                          () => {
                            // this.Filterdata();
                          },
                        );
                      } else {
                        const updatedSelectedSeasons = Array.isArray(this.state.searchDivision)
                          ? [...this.state.searchDivision]
                          : [];
                        const isSelected = Array.isArray(this.state.searchDivision)
                          ? this.state.searchDivision.includes(selectedOption.text)
                          : false;
                        if (isSelected) {
                          const index = updatedSelectedSeasons.indexOf(selectedOption.text);
                          updatedSelectedSeasons.splice(index, 1);
                        } else {
                          updatedSelectedSeasons.push(selectedOption.text);
                        }
                        this.setState(
                          {
                            searchDivision: updatedSelectedSeasons,
                            divisionSelected: '',
                            searchKey: '',
                          },
                          () => {
                            // this.Filterdata();
                          },
                        );
                      }
                    }}
                    selectedKey={this.state.searchDivision || []}
                  />
                  <Grid item lg={0.9} xs={2}></Grid>
                  <Grid item lg={0.5} xs={2}>
                    {/* <IconButton
                    title={'Clear Filter'}
                    iconProps={clearFilter}
                    styles={clearBtnStyle}
                    disabled={
                        this.state.searchDivision == null || this.state.searchDivision == undefined
                      }
                    onClick={() => {
                      if (this.state.toDate == undefined && this.state.minDate == undefined) {
                        this.refresh();
                      } else {
                        this.setState({ searchDivision: [] });
                      }
                    }}
                  /> */}
                  </Grid>
                </Stack>
              </Grid>
              <Grid item lg={3.8} xs={0} />
              <Grid item lg={1.5} xs={12}></Grid>
              <Grid item lg={0.5} xs={2}>
                <Icon
                  iconName="Search"
                  title={this.props.t('BTN_SEARCH')}
                  className="iconStyle"
                  onClick={() => {
                    if (
                      this.state.toDate != null ||
                      this.state.minDate != null ||
                      this.state.searchDivision.length != 0
                    ) {
                      this.onFilterClick();
                    }
                  }}
                />
              </Grid>
              <Grid item lg={0.5} xs={2}>
                <Icon
                  iconName="Clear"
                  title={this.props.t('BTN_CLEAR')}
                  className="iconStyle"
                  onClick={() => {
                    if (
                      this.state.toDate != null ||
                      this.state.minDate != null ||
                      this.state.searchDivision.length != 0 ||
                      this.state.searchKey != ''
                    ) {
                      this.refresh();
                      if (this.state.searchKey != '') {
                        this.onSearch(undefined);
                      }
                      this.setState({ searchKey: '', searchDivision: [] });
                      this.ClearDates();
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
              <div className={PIMcontentStyles.confirmHeader}>
                <IconButton
                  styles={CancelStyles}
                  iconProps={cancelIcon}
                  ariaLabel="Close popup modal"
                  onClick={() => {
                    this.setState({
                      recjected: false,
                    });
                  }}
                />
              </div>
              <div className={PIMcontentStyles.confirmbodyplano}>
                <Text variant="xLarge" className="apptext">{`${this.props.t(
                  'PLANOGRAM_MODEL_REJECTED_REASON',
                )}`}</Text>
              </div>
              <div className={PIMcontentStyles.body}>
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
              isOpen={this.state.discriptionVisible}
              containerClassName={PIMcontentStyles.confirmContainer}
              onDismiss={() => this.setState({ discriptionVisible: false })}
              isBlocking={false}
            >
              <div className={PIMcontentStyles.header}>
                <div className="apptext3 descHeader"> {this.props.t('MODAL_DESCRIPTION')} </div>
                <IconButton
                  styles={iconButtonStyles}
                  iconProps={cancelIcon}
                  ariaLabel="Close popup modal"
                  onClick={() => {
                    this.setState({ discriptionVisible: false });
                  }}
                />
              </div>
              <div className={PIMcontentStyles.body}>
                <TextField
                  value={this.state.descriptionValue}
                  className="textField"
                  multiline
                  autoAdjustHeight={true}
                  borderless
                  resizable={false}
                  readOnly={true}
                />
              </div>
            </Modal>
            <div className="dataTableFixedFooter">
              <DataTable
                columns={_columns}
                data={this.state.filteredRecord}
                pagination={true}
                selectableRowsHighlight
                onColumnOrderChange={handleColumnReorder}
                highlightOnHover
                fixedHeader
                striped
                fixedHeaderScrollHeight=" 68vh"
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
          </>
        </div>
      </div>
    );
  }
}
const ComponentTranslated = withTranslation()(PlanogramCommon);
function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}
export default App;