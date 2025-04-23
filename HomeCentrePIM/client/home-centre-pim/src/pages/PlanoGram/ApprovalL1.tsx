import React from 'react';
import 'react-phone-input-2/lib/style.css';
import { SearchBox } from '@fluentui/react/lib/SearchBox';
import DataTable from 'react-data-table-component';
import { DefaultButton, IconButton } from '@fluentui/react/lib/Button';
import { Text } from '@fluentui/react/lib/Text';
import {
  ContextualMenu,
  Dropdown,
  IColumn,
  IDragOptions,
  Icon,
  Modal,
  Stack,
  TextField,
} from '@fluentui/react';
import AuthManagerService from '@/services/AuthManagerService';
import ApiManager from '@/services/ApiManager';
import 'office-ui-fabric-react/dist/css/fabric.css';
import Util from '@/services/Util';
import {
  CancelButtonStyles,
  CancelStyles,
  UploadStyles,
  approveIcon,
  cancelIcon,
  iconButtonStyles,
  iconButtonStyless,
  iconButtonStylessClicked,
  iconButtonStylesss,
  iconButtonStylesssClicked,
  rejectIcon,
  rejectReasonDropdown,
} from '@/common/properties';
import { Grid } from '@mui/material';
import { DocumentCheckmarkFilled } from '@fluentui/react-icons';
import { TasklevelSequence } from '@/services/ApiClient';
import NotificationManager from '@/services/NotificationManager';
import {
  BorderShadowButton,
  columnHeader,
  PIMattributeSelect,
  PIMButtons,
  PIMcontentStyles,
  PIMfooterButtonCancel,
  PIMfooterButtonConfirm,
  PIMHearderText,
  PIMsearchBoxStyles,
} from '../PimStyles';
import jwt_decode from 'jwt-decode';

export default class ApprovalL1 extends React.Component<any, any> {
  location = window.location.pathname;
  notify = new NotificationManager();
  apiClient = new ApiManager().CreateApiClient();
  utils = new Util();
  authManager = new AuthManagerService();
  isAuthenticated = this.authManager.isAuthenticated();
  user = this.isAuthenticated ? this.authManager.getUserData() : null;
  token = sessionStorage.getItem('token');
  decode: any = jwt_decode(this.token || '');
  deptId: any = this.decode.DepartmentId.split(',').filter((id: any) => id !== '0');
  departmentId: any = parseInt(this.deptId[0], 10);
  activateRecord: any;
  isEdit: boolean | undefined;
  selectedRowData: null | undefined;
  roleColumn: IColumn[] | undefined;
  stausofRecords = true;
  isGetall: boolean | undefined;  
  
  constructor(props: any) {
    super(props);

    this.state = {
      roleId: [] = this.user?.roleId || [],
      currentRecord: [],
      enableEditButton: false,
      enableActiveButton: false,
      inActivePage: false,
      searchKey: '',
      selectedRowData: [],
      noSelection: false,
      ApprovalEnable: true,
      descriptionValue: '',
      recjectVisible: false,
      row: [],
      rejectComment: '',
      dropDownSelectedRecord: [],
      searchResult: [],
      filteredRecord: [],
      A_D_Visible: false,
      A_D_Visible1: false,
      rejectCommentList: [],
      selectedRejectComment: null,
      otherComment: false,
      minDate: undefined,
      toDate: undefined,
      searchDivision: null,
      divisionOptionlst: [],
      Approvaloption: [],
      SecoundApprovalOption: [],
      isRecord: false,
      onHover: false,
      selectionCount: 0,
      isACL2: true,
      isACL1: true,
      isDivision: true,
      ApproveSelectedClicked: false,
      divisionSelected: 0,
      actionL1Selected: 0,
      actionL2Selected: 0,
      otherSplitComment: '',
      ApprovalsLoad: false,
      departmentOption: [],
      currentPage: 1,
      rowsPerPage: 10,
    };
  }

  async componentDidMount(): Promise<void> {
    this.setState({ isRecord: true });
    const rejectComment = (await this.apiClient.getRejectReason()).result;
    var comment: any = [];
    rejectComment?.forEach((element) => {
      comment.push({ key: element.name, text: element.name });
    });
    var departmentList = (await this.apiClient.getDivision())?.result;
    var departmentOption: { key: number | undefined; text: string | undefined }[] = [];
    departmentList?.forEach((element) => {
      departmentOption.push({ key: element.id, text: element.name });
    });
    this.refresh();
    this.setState({
      rejectCommentList: comment,
      ApprovalsLoad: true,
      departmentOption: departmentOption,
      divisionOptionlst: departmentList,
    });
  }

  async refresh() {
    try {
      var data: any = [];
      this.setState({ searchKey: '' });
      const response: any = (await this.apiClient.getApprovedorRejectedList()).result;
      response?.forEach((e: any) => {
        if (e.taskStepInstanceName === 'UID First Level Approvel') {
          data.push(e);
        }
      });
      const responeWithIndex = data.map((item: any, index: any) => ({ index: index + 1, ...item }));
      this.setState({
        ApprovalEnable: true,
        filteredRecord: responeWithIndex,
        currentRecord: responeWithIndex,
        isRecord: false,
        A_D_Visible1: false,
        noSelection: !this.state.noSelection,
        selectedRowData: [],
        selectionCount: 0,
        enableEditButton: false,
        enableActiveButton: false,
        minDate: undefined,
        actionL1Selected: 0,
        actionL2Selected: 0,
        searchDivision: null,
        toDate: undefined,
        dropDownSelectedRecord: responeWithIndex,
        divisionSelected: 0,
        Division: '',
        ActionL2: '',
        ActionL1: '',
        isACL2: true,
        isDivision: true,
        isACL1: true,
        otherSplitComment: '',
      });
    } catch (e: any) {
      this.notify.showErrorNotify(e.message);
    }
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
  perpage = () => {
    const recordCont = [25, 50, 100, 150, 200];
    return recordCont;
  };
  Defaultperpage = () => {
    const recordCont = 25;
    return recordCont;
  };
  handlePageChange = (pageNumber: number) => {
    this.setState({ currentPage: pageNumber });
  };

  handleRowsPerPageChange = (newRowsPerPage: any) => {
    this.setState({ rowsPerPage: newRowsPerPage });
  };

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
    });
  }

  onApprove = async () => {
    try {
      this.setState({ approveVisible: false });
      var ids: TasklevelSequence[] = [];
      var tasklevelSeq = new TasklevelSequence();
      tasklevelSeq.taskStepInstanceId = this.state.row.taskStepInstanceId;
      tasklevelSeq.recordId = this.state.row.recordId;
      tasklevelSeq.userAction = 'Approve';
      ids.push(tasklevelSeq);
      const response = await this.apiClient.updateUserAction(ids);
      if (response.hasError) {
        this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN') || response.message);
      } else {
        this.notify.showSuccessNotify(this.props.t('APPROVED_SUCESSFULLY'));
      }
      this.refresh();
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
  };

  async onApproveBatch() {
    let { selectedRowData } = this.state;
    var ids: TasklevelSequence[] = [];

    selectedRowData.forEach((element: any) => {
      var tasklevelSeq = new TasklevelSequence();
      tasklevelSeq.taskStepInstanceId = element.taskStepInstanceId;
      tasklevelSeq.recordId = element.recordId;
      tasklevelSeq.userAction = 'Approve';
      ids.push(tasklevelSeq);
    });
    const response = await this.apiClient.updateUserAction(ids);
    this.setState({ ApprovalEnable: true, ApproveSelectedClicked: false });
    if (response.hasError) {
      this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN') || response.message);
    } else {
      this.notify.showSuccessNotify(this.props.t('APPROVED_SUCESSFULLY'));
    }
    this.refresh();
  }
  catch(error: any) {
    this.notify.showErrorNotify(error);
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
      tasklevelSeq.rejectDescription =
        selectedRejectComment.toLowerCase() == 'other' ? otherSplitComment : '';
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

  columnFilter = async (event: any, type: string) => {
    this.setState({ searchKey: '' });
    if (type == 'clear') {
      this.setState({
        dropDownSelectedRecord: this.state.currentRecord,
        divisionSelected: null,
        actionL2Selected: null,
        actionL1Selected: null,
        filteredRecord: this.state.currentRecord,
        Division: '',
        ActionL2: '',
        ActionL1: '',
        isDivision: true,
        isACL1: true,
        isACL2: true,
      });
      return;
    }
    var data: any;
    switch (type) {
      case 'division':
        data = this.state.currentRecord.filter((element: any) => {
          return element.division == event.text;
        });
        this.setState({
          divisionSelected: event.key,
          dropDownSelectedRecord: data,
          filteredRecord: data,
          Division: event,
          actionL2Selected: null,
          actionL1Selected: null,
          ActionL2: '',
          ActionL1: '',
          isDivision: false,
          isACL1: true,
          isACL2: true,
        });
        break;
    }
  };
  dragOptions: IDragOptions = {
    moveMenuItemText: 'Move',
    closeMenuItemText: 'Close',
    menu: ContextualMenu,
    dragHandleSelector: '.ms-Modal-scrollableContent > div:first-child',
  };
  render() {
    const { t } = this.props;
    var _columns: any = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{t('S_NO')}</div>,
        selector: (row: any) => row.index,
        isVisible: true,
        width: '70px',
        position: 'fixed',
      },
      {
        key: 'PlanoOrFamilyname',
        name: (
          <div className={columnHeader}>
            {new Set(this.deptId).has('4') && new Set(this.deptId).has('61')
              ? t('PLANOGRAM_TABLE_PLANO/FAMILY_NAME')
              : new Set(this.deptId).has('4')
              ? t('FAMILY_NAME')
              : t('COL_PLANOGRAM_NAME')}
          </div>
        ),
        selector: (row: any) => <span title={row.planoOrFamilyName}>{row.planoOrFamilyName}</span>,
        isVisible: true,
        width: '170px',
        position: 'fixed',
      },
      {
        name: <div className={columnHeader}>{t('PLANOGRAM_TABLE_DATE_OF_ACTION')}</div>,
        isVisible: true,
        width: '130px',
        selector: (row: any) => {
          return (
            <>
              {this.state.filteredRecord.length != 0 && (
                <Grid container direction={'row'} spacing={2}>
                  <Grid item>
                    <IconButton
                      styles={
                        (this.state.row === row && this.state.approveVisible === true) ||
                        (this.state.selectedRowData.includes(row) &&
                          this.state.ApproveSelectedClicked)
                          ? iconButtonStylesssClicked
                          : iconButtonStylesss
                      }
                      iconProps={approveIcon}
                      onClick={() => {
                        this.setState({ row: row, approveVisible: true });
                      }}
                    />
                  </Grid>
                  <Grid item>
                    <IconButton
                      styles={
                        this.state.row === row && this.state.recjectVisible === true
                          ? iconButtonStylessClicked
                          : iconButtonStyless
                      }
                      iconProps={rejectIcon}
                      onClick={() => {
                        this.setState({ row: row, recjectVisible: true });
                      }}
                    />
                  </Grid>
                </Grid>
              )}
            </>
          );
        },
      },
      {
        key: 'PlanoOrFamilyCode',
        name: <div className={columnHeader}>{this.props.t('PLANOGRAM_TABLE_DIVISION')}</div>,
        selector: (row: any) => <span title={row.division}>{row.division}</span>,
        isVisible: true,
        position: 'fixed',
      },
      {
        key: 'Type',
        name: <div className={columnHeader}>{t('PLANOGRAM_TYPE')}</div>,
        isVisible: true,
        width: '170px',
        selector: (row: any) => (
          <span
            title={
              row.requestType == 'Planogram' ? this.props.t('PLANOGRAM_NAME') : row.requestType
            }
          >
            {row.requestType == 'Planogram' ? this.props.t('PLANOGRAM_NAME') : row.requestType}
          </span>
        ),
        position: 'fixed',
      },
      {
        key: 'Description',
        name: <div className={columnHeader}>{t('PLANOGRAM_TABLE_DESCRIPTION')} </div>,
        isVisible: true,
        width: '170px',
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
        position: 'fixed',
      },
      {
        key: 'ApproverL1',
        name: <div className={columnHeader}>{t('PLANOGRAM_TABLE_APPROVER_L1')}</div>,
        isVisible: false,
        selector: (row: any) => <span title={row.actionedbyL1}>{row.actionedbyL1}</span>,
        position: 'fixed',
        width: '170px',
      },
      {
        key: 'DateofActionbyL1',
        name: <div className={columnHeader}>{t('PLANOGRAM_TABLE_DATE_OF_ACTION_BY_L1')}</div>,
        isVisible: false,
        selector: (row: any) => (
          <p>{row.l1ActionDate != null && row.l1ActionDate != '' && row.l1ActionDate}</p>
        ),
        width: '170px',
        position: 'fixed',
      },
      {
        key: 'CreatedBy',
        name: (
          <div className={columnHeader}>
            {this.departmentId == 4
              ? t('COL_FAMILY_CREATED_BY')
              : this.departmentId == 61
              ? t('COL_PLANO_CREATED_BY')
              : t('PLANOGRAM_TABLE_CREATEBY')}
          </div>
        ),
        width: '170px',
        selector: (row: any) => (
          <span title={row.planoOrFamilyCreatedBy}>{row.planoOrFamilyCreatedBy}</span>
        ),
        isVisible: true,
        position: 'fixed',
      },
      {
        key: 'DateCreated',
        name: (
          <div className={columnHeader}>
            {this.departmentId == 4
              ? t('COL_FAMILY_CREATED_DATE')
              : this.departmentId == 61
              ? t('COL_PLANO_CREATED_DATE')
              : t('PLANOGRAM_TABLE_CREATEBY')}
          </div>
        ),
        width: '190px',
        selector: (row: any) => (
          <p title={row.planoOrFamilyCreatedDate}>
            {row.planoOrFamilyCreatedDate != null &&
              row.planoOrFamilyCreatedDate != '' &&
              row.planoOrFamilyCreatedDate}
          </p>
        ),
        isVisible: true,
        position: 'fixed',
      },
    ]?.filter((item) => item.isVisible);
    const handleColumnReorder = (newColumns: any) => {
      var _column: any = [..._columns];
      if (newColumns == this.state.currentColumns) {
        this.setState({ currentColumns: _column });
      } else {
        this.setState({ currentColumns: newColumns });
      }
    };

    return (
      <div>
        <Stack>
          {/* <Text block className="text">{`${this.departmentId == 4 ? t('FAMILY_NAME') : this.departmentId == 61 ? t('COL_PLANOGRAM_NAME') : t('PLANOGRAM_TABLE_PLANO/FAMILY_NAME')} / ${this.props.t('NAV_APPROVALLIST')}`}</Text> */}
          <Text
            variant="xxLarge"
            styles={PIMHearderText}
            className="color-blue text"
          >{`${this.props.t( this.state.roleId.indexOf(2) > -1 ? 'PLANOGRAM_TABLE_APPROVAL_L2' : 'PLANOGRAM_TABLE_APPROVAL_L1')}`}</Text>
        </Stack>

        <Grid lg={12} item container spacing={2} direction={'row'}>
          <Grid item lg={2} xs={12}>
            <SearchBox
              className="searchBox"
              name="search"
              styles={PIMsearchBoxStyles}
              placeholder={
                this.departmentId == 4
                  ? t('FAMILY_NAME')
                  : this.departmentId == 61
                  ? t('COL_PLANOGRAM_NAME')
                  : t('PLANOGRAM_TABLE_PLANO/FAMILY_NAME')
              }
              onChange={(e: any) => this.onSearch(e)}
              value={this.state.searchKey}
              onClear={() => this.setState({ searchKey: '' })}
            />
          </Grid>
          <Grid item lg={2} xs={12}>
            <span title={this.props.t('APPROVE_SELECTED')}>
            <DocumentCheckmarkFilled
                className="submitIconStyle"
                // className="buttonIcon"
                onClick={() => {
                  if (this.state.ApprovalEnable) {
                    return;
                  }
                  this.setState({
                    A_D_Visible1: true,
                    enableEditButton: false,
                    enableActiveButton: false,
                    ApproveSelectedClicked: true,
                  });
                }}
                style={{
                  cursor: this.state.ApprovalEnable ? 'not-allowed' : 'pointer',
                  color: this.state.ApprovalEnable ? 'gray' : 'inherit',
                }}
              />
            </span>
          </Grid>

          <Grid item lg={2} xs={this.state.selectionCount > 0 ? 12 : 0}>
            {this.state.selectionCount > 0 && (
              <DefaultButton
                className="button rowbtn"
                text={`${this.props.t('ROW_COUNT')} | ${this.state.selectionCount}`}
                styles={PIMattributeSelect}
              />
            )}
          </Grid>
          <Grid item lg={5.4} />
          <Grid item lg={0.5} xs={2}>
            {/* <DefaultButton
                            className='button'
                            iconProps={{ iconName: 'Refresh' }}
                            text={this.props.t('ROLE_USER_REFRESH_BTN')}
                            styles={PIMButtons}
                            onClick={() => this.refresh()} /> */}
            <Icon
              iconName="Refresh"
              title={t('BTN_REFRESH')}
              className="iconStyle"
              onClick={() => this.refresh()}
            />
          </Grid>
        </Grid>
        <Modal
          isOpen={this.state.A_D_Visible1}
          containerClassName={PIMcontentStyles.confirmContaineruser1}
          isBlocking={false}
          onDismiss={() =>
            this.setState({
              A_D_Visible1: false,
              ApproveSelectedClicked: false,
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
                  ApproveSelectedClicked: false,
                });
              }}
            />
          </div>
          <div className={PIMcontentStyles.confirmbody4}>
            {this.state.inActivePage == false ? (
              <Text variant="xLarge" className="apptext">{`${this.props.t(
                'PHOTOSHOOT_TRACKING_MODEL_APPROVE',
              )}`}</Text>
            ) : (
              <Text variant="large" className="apptext1">{`${this.props.t(
                'MSG_ACTIVATE_USER_ROLE_DEPT',
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
                  text={`${this.props.t('BTN_CONFIRM')}`}
                  onClick={() => {
                    this.setState({ A_D_Visible1: false });
                    this.onApproveBatch();
                  }}
                />
              </Grid>
              <Grid item lg={0.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonCancel}
                  text={`${this.props.t('BTN_CANCEL')}`}
                  onClick={() => {
                    this.setState({ A_D_Visible1: false, ApproveSelectedClicked: false });
                  }}
                />
              </Grid>
              <Grid item lg={2} xs={12} />
            </Grid>
          </div>
        </Modal>
        <Modal
          isOpen={this.state.discriptionVisible}
          containerClassName={PIMcontentStyles.confirmContainer}
          isBlocking={false}
          onDismiss={() => {
            this.setState({ discriptionVisible: false });
          }}
        >
          <div className={PIMcontentStyles.header1}>
            <div className="apptext3"> {this.props.t('MODAL_DESCRIPTION')} </div>
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
              multiline
              className="textField"
              autoAdjustHeight={true}
              resizable={false}
              borderless
              readOnly={true}
            />
          </div>
        </Modal>
        <Modal
          isOpen={this.state.recjectVisible}
          containerClassName={PIMcontentStyles.confirmContaineruser1}
          isBlocking={false}
          dragOptions={this.dragOptions}
          onDismiss={() =>
            this.setState({
              recjectVisible: false,
              selectedRejectComment: null,
              otherComment: false,
              row: [],
            })
          }
        >
          <div className={PIMcontentStyles.confirmHeader}>
            <IconButton
              styles={CancelStyles}
              iconProps={cancelIcon}
              ariaLabel="Close popup modal"
              onClick={() => {
                this.setState({
                  recjectVisible: false,
                  selectedRejectComment: null,
                  otherComment: false,
                  row: [],
                });
              }}
            />
          </div>
          <div className={PIMcontentStyles.header}>
            <Text variant="xLarge" className="apptext3">{`${this.props.t(
              'PLANOGRAM_TABLE_MODEL_REJECT',
            )}`}</Text>
            {/* <IconButton
                            styles={iconButtonStyles}
                            iconProps={cancelIcon}
                            ariaLabel="Close popup modal"
                            onClick={() => {
                                this.setState({
                                    recjectVisible: false,
                                    selectedRejectComment: null,
                                    otherComment: false,
                                    row: [],
                                });
                            }}
                        /> */}
          </div>
          <div className={PIMcontentStyles.body}>
            <form noValidate autoComplete="off">
              <Dropdown
                className="dropdown"
                required
                label={`${this.props.t('REJECT_LABEL_COMMENT')}`}
                onChange={(e: any, item: any) =>
                  this.setState({
                    selectedRejectComment: item?.text,
                    otherComment: item.text.toLowerCase() == 'other' ? true : false,
                  })
                }
                placeholder={this.props.t('PLACEHOLDER_SELECT_AN_OPTION')}
                options={this.state.rejectCommentList}
                styles={rejectReasonDropdown}
              />
              {this.state.otherComment && (
                <TextField
                  required
                  name="otherComment"
                  className="textField"
                  label={this.props.t('LABEL_OTHER_COMMENT')}
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
              )}
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
                  onClick={() => this.onReject()}
                  disabled={
                    this.state.selectedRejectComment?.toLowerCase() === 'other'
                      ? this.state.otherSplitComment.trim() == ''
                      : this.state.selectedRejectComment == null
                  }
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
                      selectedRejectComment: null,
                      otherComment: false,
                      row: [],
                    });
                  }}
                />
              </Grid>
              <Grid item lg={2} xs={12} />
            </Grid>
          </div>
        </Modal>
        <Modal
          isOpen={this.state.approveVisible}
          containerClassName={PIMcontentStyles.confirmContaineruser1}
          isBlocking={false}
          dragOptions={this.dragOptions}
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
              onClick={() => {
                this.setState({
                  approveVisible: false,
                  row: [],
                });
              }}
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
                  onClick={() => this.onApprove()}
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
        <div className="dataTableWrapper dataTableFixedFooter">
          <DataTable
            columns={_columns}
            data={this.state.filteredRecord}
            pagination={true}
            onColumnOrderChange={handleColumnReorder}
            selectableRows={this.state.selectedRowData}
            onSelectedRowsChange={this.rowSelection}
            clearSelectedRows={this.state.noSelection}
            onChangePage={this.handlePageChange}
            // selectableRowsVisibleOnly={this.state.selectedRowData}
            // paginationPerPage={this.state.rowsPerPage}
            onChangeRowsPerPage={this.handleRowsPerPageChange}
            selectableRowsHighlight
            highlightOnHover
            fixedHeader
            striped
            fixedHeaderScrollHeight="68.5vh"
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
