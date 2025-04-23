import { DefaultButton, Modal, SearchBox, Stack } from '@fluentui/react';
import React, { Suspense } from 'react';
import { Text } from '@fluentui/react/lib/Text';
import Grid from '@mui/material/Grid';
import {
  columnHeader,
  PIMButtons,
  PIMcontentStyles,
  PIMfooterButtonCancel,
  PIMfooterButtonConfirm,
  PIMHearderText,
  PIMsearchBoxStyles,
} from '../../PimStyles';
import { withTranslation } from 'react-i18next';
import DataTable from 'react-data-table-component';
import { Link } from 'react-router-dom';
import ApiManager from '@/services/ApiManager';
import Util from '@/services/Util';
import NotificationManager from '@/services/NotificationManager';
import { baseUrl } from '@/services/Constants';
import { downloadFileDataRequest } from '@/pages/commonFunctions';

class DRviewData extends React.Component<any, any> {
  util = new Util();
  apiClient = new ApiManager().CreateApiClient();
  notify = new NotificationManager();
  token: any = sessionStorage.getItem('token');

  constructor(props: any) {
    super(props);
    this.state = {
      rowData: [],
      isRecord: false,
      noSelection: false,
      selectedRowData: [],
      selectionCount: 0,
      viewDataId: 0,
      searchKey: '',
      searchData: [],
      selectedRowId: 0,
      selectedRowValue: '',
      selectedRow: '',
      visible: false,
      columns: [],
      templateName: '',
      confirmModel: false,
      statusOption: [],
    };
  }

  async componentDidMount() {
    var status: any = [];
    const statuses = (await this.apiClient.getQcStatus()).result;
    statuses?.forEach((item: any) => {
      status.push({ key: item.id, text: item.name });
    });
    this.setState({ statusOption: status });
    this.refresh();
  }

  async refresh() {
    try {
      this.setState({ isRecord: true });
      const Ids: any = this.util.getUrlParameterId();
      const id: any = Ids.split(',');
      const viewDataId: any = parseInt(id[0]);
      const templateName: any = decodeURI(id[1]);
      this.setState({ viewDataId: viewDataId });
      const response: any = await this.apiClient.searchDataRequestItems('', 0, 0, viewDataId);
      const count: any = await this.apiClient.getDataRequestItemCountByStatus(viewDataId);
      if (!response.hasError) {
        var res: any[] = [];
        response?.result?.records?.forEach((element: any) => {
          res.push(element?.columnData);
        });
        this.columMapping(response?.result?.records);
      } else {
        this.notify.showErrorNotify(response?.message);
      }
      this.setState({ searchKey: '', templateName: templateName, confirmModel: false });
    } catch (e: any) {
      this.notify.showErrorNotify(e);
    }
    this.setState({ isRecord: false });
  }

  columMapping(data: any) {
    const columns: any[] = [];
    const rowData: any[] = [];
    var keys = Object.keys(data[0]?.columnData || '');
    keys.forEach((x: any) => {
      const self = x;
      columns.push({
        id: x,
        key: x,
        name: (
          <div className={columnHeader} title={x}>
            {x
              .split('_')
              .map((word: any) => word.charAt(0).toUpperCase() + word.slice(1))
              .join(' ')}
          </div>
        ),
        fieldName: x,
        minWidth: '250px',
        cell: (row: any, index: number, column: any, id: any) => {
          return <div>{row[column?.fieldName]?.value}</div>;
        },
        isVisible: true,
      });
    });
    data.forEach((r: any, index: number) => {
      const row: any = {};
      keys.forEach((key: any) => {
        row[key] = { value: r?.columnData[key] };
      });
      row.uid = r?.uid;
      row.id = [r.id, r.dataRequestId];
      row.qcStatusId = r?.qcStatusId;
      rowData.push(row);
    });
    this.setState({
      columns: columns,
      rowData: rowData,
      isRecord: false,
      noSelection: !this.state.noSelection,
      selectedRowData: [],
      selectionCount: 0,
    });
    this.setState({
      rowData: rowData?.map((item: any, index: any) => ({ index: index + 1, ...item })),
      searchData: rowData?.map((item: any, index: any) => ({ index: index + 1, ...item })),
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
  onSearch(e: any) {
    var newValue = e?.target.value;
    newValue = newValue == undefined ? '' : newValue;
    this.setState({ searchKey: newValue });
    var result = this.state.searchData.filter((element: any) => {
      return element?.uid
        ?.toLowerCase()
        .includes(newValue.trim().split(/ +/).join(' ').toLowerCase());
    });
    this.setState({
      rowData: result?.map((item: any, index: any) => ({ index: index + 1, ...item })),
    });
  }

  rowSelection = (e: any) => {
    this.setState({ selectedRowData: e.selectedRows, selectionCount: e.selectedCount });
  };
  handleRowDeselection = (id: any) => {
    this.setState({
      selectedRowData: this.state.selectedRowData.filter((r: any) => r.id !== id),
      selectionCount: this.state.selectedRowData.filter((r: any) => r.id !== id).length,
    });
  };
  sendToQc = async () => {
    const a = this.state.selectedRowData;
    const response: any = await this.apiClient.sendRecordToQc(a[0].id[0]);
    if (response.hasError) {
      this.notify.showSuccessNotify(this.props.t('RECORD_SENT_TO_QC'));
    } else {
      this.notify.showErrorNotify(response?.message);
    }
    this.setState({ confirmModel: false });
    this.refresh();
  };

  onMultiSelecting(event: any) {
    var { selectedDivisionIds, listOfTemplates } = this.state;
    let data = this.state.selectedDivisionIds;
    if (event.selected) {
      data.push({ key: event.key, text: event.text });
    } else {
      data = this.state.selectedDivisionIds.filter((value: any) => {
        return value.key != event.key;
      });
    }
    this.setState({ selectedDivisionIds: data, selectedTemplateId: 0 });
    var filteredArray = listOfTemplates.filter(
      (o1: any) => !data.some((o2: any) => o1.key === o2.key),
    );
  }
  downloadFile() {
    try {
      const token = this.token;
      const url = `${baseUrl}/api/DataRequest/DownloadRequestRecords?dataRequestId=${
        this.state.viewDataId
      }&${Object.entries({ token })
        .map(([key, value]) => `${key}=${value}`)
        .join('&')}`;
      downloadFileDataRequest(url, this.state.viewDataId);
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
  }

  render() {
    const { t } = this.props;
    const column = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{`${this.props.t('S_NO')}`}</div>,
        selector: (row: any) => row.index,
        isVisible: true,
        maxWidth: '100px',
      },
      {
        key: 'itemCode',
        name: <div className={columnHeader}>{`${this.props.t('ITEM_CODE')}`}</div>,
        selector: (row: any) => <span title={row.uid}>{row.uid}</span>,
        isVisible: true,
        maxWidth: '250px',
      },
      {
        key: 'status',
        name: <div className={columnHeader}>{`${this.props.t('COL_STATUS')}`}</div>,
        selector: (row: any) => (
          <span className={`statusText${row.qcStatusId}`}>
            {this.state.statusOption.find((e: any) => e.key == row.qcStatusId)?.text}
          </span>
        ),
        isVisible: true,
        maxWidth: '150px',
      },
    ];
    const columnList: any = column.concat(this.state.columns);
    return (
      <div className="layout width-100">
        <Stack>
          {/* <Text block className="text">{`${t('PIM')} / ${t('OUTPUT')} / `}<Link to='/output/datarequest'>{`${t('DATA_REQUEST')} /`}</Link>{`${t('VIEW_DATA')}`}</Text> */}
          <Text
            variant="xxLarge"
            className="color-blue text"
            styles={PIMHearderText}
          >{`${this.state.templateName}`}</Text>
        </Stack>
        <Grid lg={12} item container spacing={2} direction={'row'} style={{ marginTop: '16px' }}>
          <Grid item lg={2} xs={12}>
            <SearchBox
              className="searchBox"
              styles={PIMsearchBoxStyles}
              placeholder={`${t('ITEM_CODE')}`}
              onChange={(e: any) => this.onSearch(e)}
              value={this.state.searchKey}
              onClear={() => this.setState({ searchKey: '' })}
            />
          </Grid>
          <Grid item lg={1.5} xs={12}>
            <DefaultButton
              className="button"
              styles={PIMButtons}
              text={t('BTN_DOWNLOAD')}
              onClick={this.downloadFile.bind(this)}
            />
          </Grid>
          <Grid item lg={1.5} xs={12}>
            <DefaultButton
              className="button"
              styles={PIMButtons}
              text={t('BTN_SEND_TO_QC')}
              disabled={!(this.state.selectedRowData.length == 1)}
              onClick={() => this.setState({ confirmModel: true })}
            />
          </Grid>
          <Grid item lg={1} xs={12}>
            {/* <DefaultButton
                            styles={PIMButtons}
                            text={t('BTN_EDIT')}
                            disabled={!(this.state.selectedRowData.length > 0)}
                            onClick={() => this.setState({ visible: true })}
                        /> */}
          </Grid>
          <Grid item lg={1} xs={12}>
            {/* <DefaultButton
                            styles={PIMButtons}
                            text={t('BTN_SAVE')}
                            disabled={!(this.state.selectedRowData.length > 0)}
                            onClick={() => this.setState({ visible: true })}
                        /> */}
          </Grid>
          <Grid item lg={1.5} xs={12}>
            {/* <DefaultButton
                            styles={PIMButtons}
                            text={t('BTN_CLEAR_FILTER')}
                            disabled={!(this.state.selectedRowData.length > 0)}
                            onClick={() => this.setState({ visible: true })}
                        /> */}
          </Grid>
          <Grid item lg={2.2} xs={12} />
          <Grid item lg={1.2} xs={12}>
            <DefaultButton
              className="button"
              iconProps={{ iconName: 'Refresh' }}
              text={t('BTN_REFRESH')}
              styles={PIMButtons}
              onClick={this.refresh.bind(this)}
            />
          </Grid>
        </Grid>
        <Modal
          isOpen={this.state.confirmModel}
          containerClassName={PIMcontentStyles.confirmContainer}
          onDismiss={() =>
            this.setState({
              confirmModel: false,
            })
          }
        >
          <div className={PIMcontentStyles.confirmbody}>
            <Text variant="xLarge" className="apptext1">{`${t('CONFIRM_SEND_TO_QC')}`}</Text>
          </div>
          <div className={PIMcontentStyles.footer}>
            <Grid lg={12} item container spacing={2}>
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonConfirm}
                  text={`${t('BTN_CONFIRM')}`}
                  onClick={this.sendToQc.bind(this)}
                />
              </Grid>
              <Grid item lg={2.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonCancel}
                  text={`${t('BTN_CANCEL')}`}
                  onClick={() => {
                    this.setState({ confirmModel: false });
                  }}
                />
              </Grid>
            </Grid>
          </div>
        </Modal>
        <div className="dataTableFixedFooter">
        <DataTable
          columns={columnList}
          data={this.state.rowData}
          pagination={true}
          selectableRows={this.state.selectedRowData}
          onSelectedRowsChange={this.rowSelection}
          clearSelectedRows={this.state.noSelection}
          responsive
          striped
          highlightOnHover
          selectableRowsHighlight
          fixedHeader
          fixedHeaderScrollHeight="386px"
          paginationComponentOptions={{ rowsPerPageText: `${this.props.t('ROWS_PER_PAGE')}` }}
          paginationPerPage={this.Defaultperpage()}
          paginationRowsPerPageOptions={this.perpage()}
          noDataComponent={
            <div className="noDataWidth">
              {<DataTable columns={columnList} data={[{ '': '' }]} />}
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

const ComponentTranslated = withTranslation()(DRviewData);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
