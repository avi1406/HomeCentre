import { DefaultButton, Icon, IconButton, Modal, SearchBox, Stack } from "@fluentui/react";
import React, { Suspense } from "react";
import { Text } from '@fluentui/react/lib/Text';
import Grid from "@mui/material/Grid";
import { columnHeader, ImgIcons, ImgIcons1, PIMButtons, PIMcontentStyles, PIMHearderText, PIMsearchBoxStyles } from '../../PimStyles';
import { withTranslation } from "react-i18next";
import DataTable from "react-data-table-component";
import ApiManager from "@/services/ApiManager";
import Util from "@/services/Util";
import NotificationManager from "@/services/NotificationManager";
import { cancelIcon, iconButtonStyles } from "@/common/properties";
import QueryRequestForm from "./queryRequest";
import { baseUrl } from "@/services/Constants";

class QC extends React.Component<any, any> {
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
      searchKey: '',
      searchData: [],
      columns: [],
      templateName: '',
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
      const response: any = await this.apiClient.searchProducts('', 0, 0, undefined);
      if (!response.hasError) {
        var res: any[] = [];
        response?.result?.records?.forEach((element: any) => {
          res.push(element?.columnData);
        });
        this.columMapping(response?.result?.records);
      } else {
        this.notify.showErrorNotify(response?.message);
      }
      this.setState({ searchKey: '' });
    } catch (e: any) {
      this.notify.showErrorNotify(e);
    }
    this.setState({ isRecord: false });
  }
  perpage = () => {
    const recordCont = [25, 50, 100, 150, 200];
    return recordCont;
  };
  Defaultperpage = () => {
    const recordCont = 25;
    return recordCont;
  };
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
      editTable: false,
      rowDataShow: false,
    });
    this.setState({
      rowData: rowData?.map((item: any, index: any) => ({ index: index + 1, ...item })),
      searchData: rowData?.map((item: any, index: any) => ({ index: index + 1, ...item })),
    });
  }
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

  downloadFile() {
    try {
      const token = this.token;
      const url = `${baseUrl}/api/Product/DownloadProducts?${Object.entries({ token })
        .map(([key, value]) => `${key}=${value}`)
        .join('&')}`;
      const link = document.createElement('a');
      link.href = url;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      this.notify.showSuccessNotify(this.props.t('DOWNLOAD_STARTED'));
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
        minWidth: '250px',
      },
      {
        key: 'status',
        name: <div className={columnHeader}>{`${this.props.t('STATUS')}`}</div>,
        selector: (row: any) => (
          <span className={`statusText${row.qcStatusId}`}>
            {this.state.statusOption.find((e: any) => e.key == row.qcStatusId)?.text}
          </span>
        ),
        isVisible: true,
        minWidth: '150px',
      },
    ];
    const columnList: any = column.concat(this.state.columns);
    return (
      <div className="layout width-100">
        <Stack>
          <Grid container spacing={1} direction="row" alignItems="center" className="grid">
            <Grid item lg={11.4} xs={10.4}>
              {/* <Text block className="text">{`${t('PIM')} / ${t('OUTPUT')} / ${t('QC')}`}</Text> */}
              <Text variant="xxLarge" styles={PIMHearderText} className="color-blue text">{`${t(
                'QC',
              )}`}</Text>
            </Grid>
            <Grid item lg={0.5} xs={1.6}>
              <Icon
                iconName="Download"
                title={t('BTN_DOWNLOAD')}
                className="iconStyle"
                onClick={() => {
                  if (this.state.rowData.length !== 0) {
                    this.downloadFile();
                  }
                }}
              />
            </Grid>
          </Grid>
        </Stack>
        <Grid lg={12} item container spacing={2} direction={'row'}>
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
          {/* <Grid item lg={0.1} xs={12}></Grid> */}
          <Grid item lg={1.4} xs={12}>
            <DefaultButton
              className="button"
              styles={PIMButtons}
              text={t('BTN_RAISE_QUERY')}
              disabled={!(this.state.selectedRowData.length > 0)}
              onClick={() => this.setState({ visible: true })}
            />
          </Grid>
          {/* <Grid item lg={1} xs={6}>
            <DefaultButton
              styles={PIMButtons}
              text={t('BTN_PASS_QC')}
              disabled={!(this.state.selectedRowData.length > 0)}
              onClick={() => this.setState({ visible: true })}
            />
          </Grid> */}
          {/* <Grid item lg={1} xs={6}>
            <DefaultButton
              styles={PIMButtons}
              text={t('BTN_SAVE')}
              disabled={!(this.state.selectedRowData.length > 0)}
              onClick={() => this.setState({ visible: true })}
            />
          </Grid> */}
          {/* <Grid item lg={1.5} xs={6}>
            <DefaultButton
              styles={PIMButtons}
              text={t('BTN_CLEAR_FILTER')}
              disabled={!(this.state.selectedRowData.length > 0)}
              onClick={() => this.setState({ visible: true })}
            />
          </Grid> */}
          <Grid item lg={7.92} xs={0} />
          <Grid item lg={0.5} xs={6}>
            <Icon
              iconName="Refresh"
              title={t('BTN_REFRESH')}
              className="iconStyle"
              onClick={this.refresh.bind(this)}
            />
          </Grid>
        </Grid>
        <Modal
          isOpen={this.state.visible}
          containerClassName={PIMcontentStyles.tableContainer}
          isBlocking={true}
        >
          <div className={PIMcontentStyles.header}>
            <span className="apptext">{t('QUERY_REQUEST')}</span>
            {/* <IconButton
                  styles={iconButtonStyles}
                  iconProps={cancelIcon}
                  ariaLabel="Close popup modal"
                  onClick={() => {
                    this.setState({
                      visible: false,
                      noSelection: !this.state.noSelection,
                      selectedRowData: [],
                      selectionCount: 0,
                    });
                  }}
                /> */}
          </div>
          <div className={PIMcontentStyles.body}>
            <QueryRequestForm
              removeSelection={this.handleRowDeselection}
              rowData={this.state.selectedRowData}
              CloseModal={() =>
                this.setState({
                  visible: false,
                  noSelection: !this.state.noSelection,
                  selectedRowData: [],
                  selectionCount: 0,
                })
              }
            />
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

const ComponentTranslated = withTranslation()(QC);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
