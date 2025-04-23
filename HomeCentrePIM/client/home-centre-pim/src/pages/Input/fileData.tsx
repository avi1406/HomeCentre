import { DefaultButton, Dropdown, Icon, Modal, SearchBox, Stack } from '@fluentui/react';
import React, { Suspense } from 'react';
import { Text } from '@fluentui/react/lib/Text';
import Grid from '@mui/material/Grid';
import {
  columnHeader,
  PIMButtons,
  PIMcontentStyles,
  PIMdropdownStyles,
  PIMHearderText,
  PIMsearchBoxStyles,
} from '../PimStyles';
import { withTranslation } from 'react-i18next';
import DataTable from 'react-data-table-component';
import { Link } from 'react-router-dom';
import ApiManager from '@/services/ApiManager';
import Util from '@/services/Util';
import UploadData from './UploadData';
import NotificationManager from '@/services/NotificationManager';

const selectOption: any = [
  { key: 0, text: 'All' },
  { key: 1, text: 'Error Data' },
];

class FileData extends React.Component<any, any> {
  util = new Util();
  apiClient = new ApiManager().CreateApiClient();
  notify = new NotificationManager();

  constructor(props: any) {
    super(props);
    this.state = {
      rowData: [],
      isRecord: false,
      noSelection: false,
      selectedRowData: [],
      selectionCount: 0,
      uploadedFileId: 0,
      msgVisible: false,
      message: '',
      messageType: false,
      searchKey: '',
      searchData: [],
      rowDataShow: false,
      selectedRowId: 0,
      selectedRowValue: '',
      selectedRow: '',
      editTable: false,
      visible: false,
      columns: [],
      searchRowData: [],
      selectedData: 0,
      errorData: '',
      orderBy: [],
    };
  }

  async componentDidMount() {
    this.refresh();
  }

  async refresh() {
    this.setState({ isRecord: true });
    const UIDList: any = [];
    const urldatas: any = this.util.getUrlParameterId();
    const urldata: any = urldatas.split(',');
    const uploadedFileId: any = parseInt(urldata[0]);
    const templateName: any = decodeURI(urldata[1]);
    this.setState({ uploadedFileId: uploadedFileId });
    const searchKey = '';
    const response = await this.apiClient.searchUploadedFileRecords(
      searchKey,
      0,
      0,
      parseInt(uploadedFileId),
    );
    const configValue: any = await this.apiClient.getAppConfig();
    const appConfiguration = templateName.toLowerCase().includes('buyer template 1')
      ? JSON.parse(configValue?.result?.find((e: any) => e.name == 'BuyerTemplate1order').jsonValue)
      : templateName.toLowerCase().includes('buyer template 2')
      ? JSON.parse(configValue?.result?.find((e: any) => e.name == 'BuyerTemplate2order').jsonValue)
      : templateName.toLowerCase().includes('qc template')
      ? JSON.parse(configValue?.result?.find((e: any) => e.name == 'QCTemplate').jsonValue)
      : templateName.toLowerCase().includes('fab template')
      ? JSON.parse(configValue?.result?.find((e: any) => e.name == 'FABOrder').jsonValue)
      : templateName.toLowerCase().includes('packaging designer template')
      ? JSON.parse(
          configValue?.result?.find((e: any) => e.name == 'PackagingDesignerOrder').jsonValue,
        )
      : templateName.toLowerCase().includes('uid template')
      ? JSON.parse(configValue?.result?.find((e: any) => e.name == 'UIDtemplateorder').jsonValue)
      : [];
    appConfiguration?.forEach((element: any) => {
      UIDList.push(element.text);
    });
    if (!response.hasError) {
      var res: any[] = [];
      var err: any[] = [];
      response?.result?.records?.forEach((element) => {
        res.push(element?.columnData);
      });
      response?.result?.records?.forEach((error:any) => {
        err.push(error?.errorData)
      })
      this.setState({
        searchRowData: response?.result?.records,
        orderBy: UIDList,
        searchKey: '',
        isRecord: false,
        selectedData: 0,
        allErrors: err,
      });
      this.columMapping(response?.result?.records, UIDList);
    } else {
      this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN'));
    }
  }
  perpage = () => {
    const recordCont = [25, 50, 100, 150, 200];
    return recordCont;
  };
  Defaultperpage = () => {
    const recordCont = 25;
    return recordCont;
  };
  columMapping(data: any, orderby?: any) {
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
        width: '250px',
        cell: (row: any, index: number, column: any, id: any) => {
          return (
            <div className={row[column.fieldName]?.error && 'PIM_FileError'}>
              {row[column?.fieldName]?.value}
              {row[column?.fieldName]?.value && <br />}
            </div>
          );
        },
        isVisible: true,
      });
    });
    let errData = '';
    let i = 0;
    for (var j = 0; j < data.length; j++) {
      var errKey = data[j]?.errorData;
      for (let key in errKey) {
        errData += errKey[key].join(' ') + '\n \n';
        if (i < Object.keys(errKey).length - 1) {
          errData += ' ||~ ';
        }
        i++;
      }
    }
    data.forEach((r: any, index: number) => {
      const row: any = {};
      keys.forEach((key: any) => {
        row[key] = { value: r?.columnData[key], error: r?.errorData[key] };
      });
      rowData.push(row);
    });
    const col = this.customSortingFunction(columns, orderby);
    this.setState({
      errorData: errData,
      columns: col,
      rowData: rowData,
      searchData: rowData,
      isRecord: false,
      noSelection: !this.state.noSelection,
      selectedRowData: [],
      selectionCount: 0,
      editTable: false,
      rowDataShow: false,
    });
  }

  customSortingFunction(files: any, keywords: any) {
    const keywordFiles = keywords.map(() => []);
    const otherFiles = [];
    for (const fileStr of files) {
      const file = fileStr.fieldName.toString().toLowerCase();
      let matched = false;
      for (let i = 0; i < keywords.length; i++) {
        const keyword = keywords[i];
        if (file == keyword) {
          keywordFiles[i].push(fileStr);
          matched = true;
          break;
        }
      }
      if (!matched) {
        otherFiles.push(fileStr);
      }
    }
    keywordFiles.forEach((filesArr: any) => filesArr.sort());
    otherFiles.sort();
    return keywordFiles.flat().concat(otherFiles);
  }

  onSearch(e: any) {
    var newValue = e?.target.value;
    newValue = newValue == undefined ? '' : newValue;
    this.setState({ searchKey: newValue });
    var result = this.state.searchData.filter((element: any) => {
      const planogramCode = String(element['Planogram/Family Code']?.value);
      return (
        planogramCode.toLowerCase().includes(newValue.trim().split(/ +/).join(' ').toLowerCase()) ||
        element['Uid']?.value
          ?.toLowerCase()
          .includes(newValue.trim().split(/ +/).join(' ').toLowerCase())
      );
    });
    this.setState({ rowData: result });
    this.setState({
      rowData: result?.map((item: any, index: any) => ({ index: index + 1, ...item })),
    });
  }

  onFilter(selectedOption: any) {
    this.setState({ selectedData: selectedOption.key });
    var { searchRowData } = this.state;
    if (selectedOption.key == 1) {
      searchRowData = searchRowData.filter((element: any) => {
        return JSON.stringify(element.errorData) != '{}';
      });
    }
    this.columMapping(searchRowData, this.state.orderBy);
  }

  render() {
    const { t } = this.props;
    const errorColumn = [
      {
        id: 'errorData',
        name: <div className={columnHeader}>{this.props.t('ERROR_DATA')}</div>,
        selector: (row: any, index:any) => {
          var order: any = [];
          this.state.orderBy?.forEach((key: any) => {
            order.push(key);
          });
          // let errorSet = new Set();
          // for (const property in row) {
          //   if (row.hasOwnProperty(property)) {
          //     const error = row[property].error;
          //     if (error && error.length > 0) {
          //       error.forEach((msg: any) => errorSet.add(msg));
          //     }
          //   }
          // }
          // const errors: any = Array.from(errorSet);
          // const sortedErrors: any = errors.sort((errorA: any, errorB: any) => {
          //   const propertyNameA = order.find((property: any) => errorA.includes(property));
          //   const propertyNameB = order.find((property: any) => errorB.includes(property));
          //   const indexA = order.indexOf(propertyNameA);
          //   const indexB = order.indexOf(propertyNameB);
          //   return indexA - indexB;
          // });
          const rowKeys = Object.keys(row);
          const sortedErrors1: any[] = order.map((key: any) => {
            const trimmedKey = key.trim().toLowerCase();
            const rowData = rowKeys.find((rowKey) => rowKey.toLowerCase() === trimmedKey);
            if (rowData) {
              const error = row[rowData].error;
              if (error && error.length > 0) {
                return error;
              }
            }
            return null;
          });
          const nonNullErrors = sortedErrors1.filter((error) => error !== null);
          const hierarchyError =
            this.state.allErrors &&
            this.state.allErrors.length > 0 &&
            this.state.allErrors[index].Hierarchy
              ? this.state.allErrors[index].Hierarchy
              : null;
          if (hierarchyError) {
              nonNullErrors.push(hierarchyError);
          }
          const errorString =
            nonNullErrors.length === 0
              ? ''
              : nonNullErrors
                  ?.join(' || ')
                  ?.match(/.{1,50}(\s|$)/g)
                  ?.join('~') + '~';
          // const errorString =
          //   sortedErrors1.length == 0
          //     ? ''
          //     : sortedErrors1
          //         ?.join(' || ')
          //         ?.match(/.{1,50}(\s|$)/g)
          //         ?.join('~') + '~';
          return (
            <div className="PIM_FileError">
              {errorString.split('~').map((line: any, index: any) => (
                <React.Fragment key={index}>
                  {line}
                  <br />
                </React.Fragment>
              ))}
            </div>
          );
        },
      },
    ];
    const dataColumn =
      this.state.errorData != '' ? errorColumn.concat(this.state.columns) : this.state.columns;
    return (
      <div className="layout width-100">
        <div className="bg-Color">
          <Stack>
            {/* <Text block className="text">{`${t('PIM')} /`}<Link to='/pim/input'>{`${t('INPUT_DATA')}`}</Link></Text> */}
            <Text variant="xxLarge" styles={PIMHearderText} className="color-blue text">{`${t(
              'INPUT_DATA',
            )}`}</Text>
          </Stack>
          <Grid lg={12} item container spacing={2} direction={'row'}>
            <Grid item lg={2} xs={12}>
              <SearchBox
                className="searchBox"
                styles={PIMsearchBoxStyles}
                placeholder={`${t('PLANOGRAM_NAME_SEARCH')}`}
                onChange={(e: any) => this.onSearch(e)}
                value={this.state.searchKey}
                onClear={() => this.setState({ searchKey: '' })}
              />
            </Grid>
            <Grid item lg={1.5} xs={12}>
              <Dropdown
                className="dropdown"
                placeholder="Select Dropdown"
                styles={PIMdropdownStyles}
                selectedKey={this.state.selectedData}
                defaultValue={0}
                onChange={(event, selectedOption) => this.onFilter(selectedOption)}
                options={selectOption}
              />
            </Grid>
            <Grid item lg={7.4} xs={12} />
            <Grid item lg={0.5} xs={12}>
              <Link
                to={`/DataSync/template`}
                onClick={() => {
                  const clicked = 'true';
                  sessionStorage.setItem('Tableclicked', clicked);
                }}
              >
                <Icon
                  iconName="Back"
                  title={t('BTN_BACK')}
                  className="iconStyle"
                  // onClick={this.refresh.bind(this)}
                />
              </Link>
            </Grid>
            <Grid item lg={0.5} xs={12}>
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
            containerClassName={PIMcontentStyles.ExcelContainer}
            onDismiss={() =>
              this.setState({
                visible: false,
              })
            }
          >
            <h2 style={{ color: 'blue', marginLeft: '-70%', fontWeight: 'FontWeights.semibold' }}>
              Upload
            </h2>
            <div className={PIMcontentStyles.body}>
              <UploadData ClosePopup={() => this.setState({ visible: false })} />
            </div>
          </Modal>
          <div className="dataTableFixedFooter">
          <DataTable
            className="exclude-table"
            columns={dataColumn}
            data={this.state.rowData}
            pagination={true}
            responsive
            striped
            highlightOnHover
            fixedHeader
            fixedHeaderScrollHeight="65vh"
            paginationComponentOptions={{ rowsPerPageText: `${this.props.t('ROWS_PER_PAGE')}` }}
            paginationPerPage={this.Defaultperpage()}
            paginationRowsPerPageOptions={this.perpage()}
            noDataComponent={
              <div className="noDataWidth">
                {<DataTable columns={dataColumn} data={[{ '': '' }]} />}
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

const ComponentTranslated = withTranslation()(FileData);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
