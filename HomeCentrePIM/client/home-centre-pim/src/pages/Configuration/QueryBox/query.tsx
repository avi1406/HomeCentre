import { DefaultButton, Icon, IconButton, IDropdownOption, Modal, Stack } from "@fluentui/react";
import React, { Suspense } from "react";
import DataTable from "react-data-table-component";
import { Text } from '@fluentui/react/lib/Text';
import jwt_decode from 'jwt-decode';
import { Grid } from "@mui/material";
import { PIMButtons, PIMcontentStyles, columnHeader, PIMfooterButtonConfirm, PIMfooterButtonCancel, boxStyles, PIMHeaderButtons, PIMHearderText } from "../../PimStyles";
import ApiManager from "@/services/ApiManager";
import AuthManagerService from "@/services/AuthManagerService";
import { withTranslation } from "react-i18next";
import NotificationManager from "@/services/NotificationManager";
import { CancelButtonStyles, cancelIcon } from "@/common/properties";
import { iconButtonStyles } from "@/common/Button";
import QcChat from "./queryChat";

class QueryBox extends React.Component<any, any> {
  authManager = new AuthManagerService();
  notify = new NotificationManager();
  apiClient = new ApiManager().CreateApiClient();
  isAuthenticated = this.authManager.isAuthenticated();
  user = this.isAuthenticated ? this.authManager.getUserData() : null;
  token = sessionStorage.getItem('token');
  decode: any = jwt_decode(this.token || '');
  rolelvl: any = this.decode.RoleLevel;
  roleId: any[] = this.user?.roleId || [];
  isOutput =
  (this.rolelvl == 'Level 2' && this.roleId.indexOf(2) > -1) ||
    this.roleId.indexOf(7) > -1 ||
    this.roleId.indexOf(8) > -1 ||
    this.roleId.indexOf(9) > -1 ||
    this.roleId.indexOf(11) > -1 ||
    this.roleId.indexOf(12) > -1
      ? true
      : false;
  QcStatus: IDropdownOption[] = [
    { key: 10, text: 'Pending' },
    { key: 30, text: 'Resolved' },
  ];
  constructor(props: any) {
    super(props);
    this.state = {
      queryData: [],
      selectionCount: 0,
      selectedRowData: [],
      noSelection: false,
      isRecord: false,
      isActive: true,
      searchData: [],
      chatVisible: false,
      rowData: [],
      isAssigned: true,
      confirmPass: false,
    };
  }

  componentDidMount(): void {
    this.refresh();
  }

  async refresh(isAssign?: boolean) {
    this.setState({ isRecord: true });
    const isAssigned = isAssign == undefined ? true : isAssign;
    const response = !isAssigned
      ? (await this.apiClient.searchQueries('', 0, 0, undefined)).result?.records
      : (await this.apiClient.searchMyQueries('', 0, 0, 10)).result?.records;
    this.setState({
      queryData: response,
      noSelection: !this.state.noSelection,
      selectedRowData: [],
      selectionCount: 0,
      isRecord: false,
      chatVisible: false,
      confirmPass: false,
    });
    this.setState({
      queryData: response?.map((item: any, index: any) => ({ index: index + 1, ...item })),
      searchData: response?.map((item: any, index: any) => ({ index: index + 1, ...item })),
    });
  }

  rowSelection = (e: any) => {
    this.setState({ selectedRowData: e.selectedRows, selectionCount: e.selectedCount });
  };
  qcPass = async () => {
    const selectedRecordIds = [];
    for (let i = 0; i < this.state.selectedRowData?.length; i++) {
      selectedRecordIds.push(this.state.selectedRowData[i].id);
    }
    const response = await this.apiClient.resolveQuery(selectedRecordIds);
    if (response.hasError) {
      this.notify.showErrorNotify(this.props.t('UNKNOWN_ERROR'));
    } else {
      this.notify.showSuccessNotify(this.props.t('QUERY_RESOLVED'));
    }
    this.refresh(false);
  };
  onResponse = () => {
    this.refresh(this.state.isAssigned);
  };
  perpage = () => {
    const recordCont = [25, 50, 100, 150, 200];
    return recordCont;
  };
  Defaultperpage = () => {
    const recordCont = 25;
    return recordCont;
  };

  render() {
    const { t } = this.props;
    const columns: any = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{`${this.props.t('S_NO')}`}</div>,
        selector: (row: any) => row.index,
        isVisible: true,
      },
      {
        key: 'description',
        name: <div className={columnHeader}>{`${this.props.t('COL_DESCRIPTION')}`}</div>,
        selector: (row: any) => row.description,
        isVisible: true,
        minwidth: '500px',
      },
      {
        key: 'dateCreated',
        name: (
          <div className={columnHeader}>
            {this.state.isAssigned == true
              ? `${this.props.t('COL_DATE_RECIEVED')}`
              : `${this.props.t('COL_DATE_CREATED')}`}
          </div>
        ),
        selector: (row: any) =>
          row.dateCreated != null &&
          row.dateCreated != '' &&
          new Date(row.dateCreated).toLocaleDateString('en-GB'),
        isVisible: true,
      },
      {
        key: 'status',
        name: <div className={columnHeader}>{`${this.props.t('COL_STATUS')}`}</div>,
        selector: (row: any) => (
          <span className={row.productQueryStatusId == 10 ? 'pendingQuery' : 'resolvedQuery'}>
            {this.QcStatus.find((e: any) => e.key == row.productQueryStatusId)?.text}
          </span>
        ),
        isVisible: true,
      },
      {
        key: 'inbox',
        name: <div className={columnHeader}>{`${this.props.t('INBOX')}`}</div>,
        selector: (row: any) => (
          <DefaultButton
            className="queryDataBtn"
            onClick={() => this.setState({ chatVisible: true, rowData: row })}
            text={this.props.t('VIEW_QUERY')}
          />
        ),
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
    return (
      <div className="layout width-100">
        <Stack>
          {/* <Text block className="text">{`${t('PIM')} / ${t('QUERY_BOX')}`}</Text> */}
          <Text variant="xxLarge" styles={PIMHearderText} className="color-blue text">{`${t(
            'QUERY_BOX',
          )}`}</Text>
        </Stack>
        <Grid lg={12} item container spacing={2} direction={'row'}>
          <Grid item lg={2} xs={6}>
            {this.state.isAssigned == true ? (
              <DefaultButton
                className="button"
                text={`${t('ASSIGNED')}`}
                styles={PIMHeaderButtons}
              />
            ) : (
              <DefaultButton
                className="button"
                text={this.props.t('ASSIGNED')}
                styles={PIMButtons}
                onClick={() => {
                  this.setState({ isAssigned: true });
                  this.refresh(true);
                }}
              />
            )}
          </Grid>
          <Grid item lg={2} xs={6}>
            {this.state.isAssigned != true ? (
              <DefaultButton className="button" text={t('CREATED')} styles={PIMHeaderButtons} />
            ) : (
              <DefaultButton
                className="button"
                text={t('CREATED')}
                styles={PIMButtons}
                onClick={() => {
                  this.setState({ isAssigned: false });
                  this.refresh(false);
                }}
                disabled={!this.isOutput}
              />
            )}
          </Grid>
          <Grid item lg={7.8} xs={0}></Grid>
        </Grid>
        <div style={boxStyles}>
          <Grid lg={12} item container spacing={2} direction={'row'}>
            <Grid item lg={9.4} xs={0} />
            <Grid item lg={1.3} xs={6}>
              {this.state.isAssigned == false && (
                <DefaultButton
                  className="button"
                  text={t('BTN_PASS_QC')}
                  styles={PIMButtons}
                  onClick={() => this.setState({ confirmPass: true })}
                  disabled={this.state.selectionCount > 0 ? false : true}
                />
              )}
            </Grid>
            <Grid item lg={0.8} xs={0}></Grid>
            <Grid item lg={0.5} xs={2}>
              {/* <DefaultButton
                                className='button'
                                iconProps={{ iconName: 'Refresh' }}
                                text={t('BTN_REFRESH')}
                                styles={PIMButtons}
                                onClick={() => this.refresh(this.state.isAssigned)}
                            /> */}
              <Icon
                iconName="Refresh"
                title={t('BTN_REFRESH')}
                className="iconStyle"
                onClick={() => {
                  this.refresh(this.state.isAssigned);
                }}
              />
            </Grid>
          </Grid>
          <Modal
            isOpen={this.state.confirmPass}
            containerClassName={PIMcontentStyles.confirmContainer}
            onDismiss={() =>
              this.setState({
                confirmPass: false,
              })
            }
          >
            <div className={PIMcontentStyles.confirmHeader}>
              {/* <IconButton
                styles={CancelButtonStyles}
                iconProps={cancelIcon}
                ariaLabel="Close popup modal"
                onClick={() => {
                  this.setState({ confirmPass: false });
                }}
              /> */}
            </div>
            <div className={PIMcontentStyles.confirmbody}>
              <Text variant="xLarge" className="apptext1">{`${t('CONFIRM_PASS_QC')}`}</Text>
            </div>
            <div className={PIMcontentStyles.footer}>
              <Grid lg={12} item container spacing={2}>
                <Grid item lg={1.5} xs={12} />
                <Grid item lg={4} xs={12}>
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonConfirm}
                    text={`${t('BTN_CONFIRM')}`}
                    onClick={this.qcPass}
                  />
                </Grid>
                <Grid item lg={0.5} xs={12} />
                <Grid item lg={4} xs={12}>
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonCancel}
                    text={`${t('BTN_CANCEL')}`}
                    onClick={() => {
                      this.setState({ confirmPass: false });
                    }}
                  />
                </Grid>
                <Grid item lg={2} xs={12} />
              </Grid>
            </div>
          </Modal>
          <Modal
            isOpen={this.state.chatVisible}
            containerClassName={PIMcontentStyles.tableContainer}
            isBlocking={true}
            onDismiss={() =>
              this.setState({
                chatVisible: false,
              })
            }
          >
            <div className={PIMcontentStyles.header}>
              {/* <IconButton
                styles={iconButtonStyles}
                iconProps={cancelIcon}
                ariaLabel="Close popup modal"
                onClick={() => {
                  this.setState({ chatVisible: false });
                }}
              /> */}
            </div>
            <div className={PIMcontentStyles.body}>
              <QcChat
                rowData={this.state.rowData}
                QcStatus={this.QcStatus}
                isAssign={this.state.isAssigned}
                isOutput={this.isOutput}
                onLoad={() => this.refresh(false)}
              />
            </div>
          </Modal>
          <div className="dataTableFixedFooter">
          <DataTable
            columns={columns}
            data={this.state.queryData}
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
            fixedHeaderScrollHeight="68.2"
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

const ComponentTranslated = withTranslation()(QueryBox);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
