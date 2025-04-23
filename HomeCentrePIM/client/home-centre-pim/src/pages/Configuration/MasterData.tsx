import React, { Suspense } from 'react';
import { withTranslation } from 'react-i18next';
import { Text } from '@fluentui/react/lib/Text';
import { Grid } from '@mui/material';
import {
  DefaultButton,
  Dropdown,
  Icon,
  IconButton,
  Modal,
  PrimaryButton,
  ProgressIndicator,
  Stack,
} from '@fluentui/react';
import {
  ImgIcons,
  ImgIcons1,
  PIMButtons,
  PIMHearderText,
  PIMcontentStyles,
  PIMdropdownStyles,
  columnHeader,
} from '../PimStyles';
import AuthManagerService from '@/services/AuthManagerService';
import NotificationManager from '@/services/NotificationManager';
import ApiManager from '@/services/ApiManager';
import axios from 'axios';
import DataTable from 'react-data-table-component';
import { iconButtonStyles } from '@/common/Button';
import { cancelIcon } from '@/common/properties';
import UploadData from '../Input/UploadData';
import { BlockBlobClient } from '@azure/storage-blob';
import { baseUrl } from '@/services/Constants';
import { downloadFileMarster } from '../commonFunctions';

class MasterData extends React.Component<any, any> {
  authManager = new AuthManagerService();
  notify = new NotificationManager();
  apiClient = new ApiManager().CreateApiClient();

  constructor(props: any) {
    super(props);
    this.state = {
      MasterDataFiles: [],
      selectedOption: 0,
      columns: [],
      masterDatas: [],
      isRecord: false,
      isDataShow: false,
      visible: false,
      progressPercent: 0,
      selectedName: '',
      percentageComplete: '',
    };
  }

  async componentDidMount() {
    this.setState({ isRecord: true });
    this.Refresh();
  }

  Refresh = async () => {
    const data: any = await this.apiClient.getAppConfig();
    const Value: any = data?.result?.find((e: any) => e.name == 'MasterDataSet')?.jsonValue;
    const Options: any = Value == undefined ? [] : JSON.parse(Value);
    this.setState({
      MasterDataFiles: Options,
      selectedOption: '',
      masterDatas: [],
      columns: [],
      isRecord: false,
      isDataShow: false,
    });
  };

  GetListData = async (id: any) => {
    this.setState({ isRecord: true, isDataShow: true });
    const data: any = await this.apiClient.getMasterData(id);
    this.columnMapping(data);
  };
  perpage = () => {
    const recordCont = [25, 50, 100, 150, 200];
    return recordCont;
  };
  Defaultperpage = () => {
    const recordCont = 25;
    return recordCont;
  };
  columnMapping = async (data: any) => {
    if (data?.result?.length == 0) return this.setState({ isRecord: false });
    const columns: any[] = [];
    const rowData: any[] = [];
    var keys = Object.keys(data?.result == undefined ? '' : data?.result[0]);
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
          const cellContent = (row[column?.fieldName] || '').toString();
          return (
            <span title={cellContent}>
              <div>{cellContent}</div>
            </span>
          );
        },
        isVisible: true,
      });
    });
    data?.result?.forEach((r: any, index: number) => {
      const row: any = {};
      keys.forEach((key: any) => {
        row[key] = r?.[key];
      });
      rowData.push(row);
    });
    this.setState({ columns: columns, masterDatas: rowData, isRecord: false });
  };

  optionsSet = async (e: any) => {
    this.setState({ selectedOption: e.key, selectedName: e.text });
    this.GetListData(e.key);
  };
  DownloadMasterData = async (e: any, fileName: any) => {
    try {
      axios(
        baseUrl + `/api/Template/DownloadMasterDataAsExcel?masterDataID=${e}&name=${fileName}`,
        {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json;',
          },
        },
      ).then((response: any) => {
        if (response.data.hasError === true) {
          return this.notify.showErrorNotify(response.data.message);
        } else {
          const url = `${baseUrl}/api/Template/DownloadMasterDataAsExcel?masterDataID=${e}&name=${fileName}`;
          downloadFileMarster(url, fileName);
        }
      });
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
  };

  removeParentheses = (fileName: any) => {
    return fileName.replace(/\([^()]*\)/g, '');
  };

  async onUpload(files: any) {
    try {
      if (!files) {
        return new Promise(() => {});
      }
      var f: any = files[0];
      this.setState({ visible: false, ProgressVisible: true, percentageComplete: f.name });
      const uri = (await this.apiClient.getAuthorizedUrlForWrite(f?.name || ''))?.result;

      const blobClient = new BlockBlobClient(uri || '');
      blobClient
        .uploadData(f, {
          onProgress: (observer) => {
            var progress = Math.round(observer.loadedBytes / f.size);
            this.setState({ progressPercent: progress });
          },
          blobHTTPHeaders: {
            blobContentType: f.type,
          },
        })
        .then(async (blobClientResponse) => {
          this.setState({ ProgressVisible: false });
          this.apiClient.masterDataUpload(uri, this.state.selectedOption).then((r) => {
            if (r.hasError) {
              this.notify.showErrorNotify(
                r.message || this.props.t('MSG-DATA-FILEUPLOAD-COMMON-ERROR'),
              );
            } else {
              this.notify.showSuccessNotify(this.props.t('MSG-DATA-FILEUPLOAD-COMMON-SUCCESS'));
              this.GetListData(this.state.selectedOption);
              this.setState({ isTemplateFile: true });
            }
          });
        });
    } catch (error) {
      this.setState({ ProgressVisible: false });
      this.notify.showErrorNotify(this.props.t('MSG-DATA-FILEUPLOAD-COMMON-ERROR'));
    }
  }

  render() {
    const { t } = this.props;
    return (
      <div className="layout width-100">
        <Text variant="xxLarge" styles={PIMHearderText} className="color-blue text">
          {t('MASTER_DATA')}
        </Text>
        <Grid lg={12} marginBottom={1.3} item container spacing={2} direction={'row'}>
          <Grid item lg={2} xs={0}>
            {/* <Text variant="xLarge" className="color-blue text">{t('MASTER_DATA')}</Text> */}
          </Grid>
          <Grid item lg={2} xs={6}>
            <Dropdown
              className="dropdown"
              placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
              defaultSelectedKey={0}
              options={this.state.MasterDataFiles}
              styles={PIMdropdownStyles}
              onChange={(e: any, selectedOption: any) => this.optionsSet(selectedOption)}
            />
          </Grid>
          {/* <Grid item lg={0.9} xs={12} /> */}
          <Grid item lg={0.5} xs={2}>
            {/* <PrimaryButton
                  text={t('BTN_DOWNLOAD')}
                  iconProps={{ iconName: 'Download' }}
                  disabled={this.state.selectedOption == '' ? true : false}
                  onClick={() =>
                    this.DownloadMasterData(this.state.selectedOption, this.state.selectedName)
                  }
                  styles={PIMButtons}
                /> */}
            <Icon
              iconName="Download"
              title={t('BTN_DOWNLOAD')}
              className="iconStyle"
              onClick={() => {
                if (this.state.selectedOption !== '') {
                  this.DownloadMasterData(this.state.selectedOption, this.state.selectedName);
                }
              }}
            />
          </Grid>
          {/* <Grid item lg={2} xs={6}> */}
          {/* <PrimaryButton text={t('BTN_UPLOAD')}
                            iconProps={{ iconName: 'Upload' }}
                            disabled={this.state.selectedOption == "" ? true : false}
                            onClick={() => this.setState({ visible: true })}
                            styles={PIMButtons}
                        /> */}
          {/* </Grid> */}
          <Grid item lg={7} />
          <Grid item lg={0.5} xs={2}>
            <Icon
              iconName="Refresh"
              title={t('BTN_REFRESH')}
              className="iconStyle"
              onClick={() => {
                this.Refresh();
              }}
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
          <div className={PIMcontentStyles.header}>
            <div className="apptext">
              <h2>{t('UPLOAD')}</h2>
            </div>
            <IconButton
              styles={iconButtonStyles}
              iconProps={cancelIcon}
              ariaLabel="Close popup modal"
              onClick={() => {
                this.setState({ visible: false });
              }}
            />
          </div>
          <div className={PIMcontentStyles.body}>
            <UploadData
              ClosePopup={() => this.setState({ visible: false })}
              UploadData={(file: any) => this.onUpload(file)}
            />
          </div>
        </Modal>
        <Modal
          isOpen={this.state.ProgressVisible}
          containerClassName={PIMcontentStyles.progressBarModal}
          onDismiss={() =>
            this.setState({
              ProgressVisible: false,
            })
          }
        >
          <div style={{ marginTop: '8px' }}>
            <Text variant="small" className="text">
              <b>{this.state.percentageComplete}</b>
            </Text>
          </div>
          <ProgressIndicator barHeight={20} percentComplete={this.state.progressPercent} />
        </Modal>
        {this.state.isDataShow && (
          <div className="dataTableFixedFooter">
          <DataTable
            className='masterdata'
            columns={this.state.columns}
            data={this.state.masterDatas}
            pagination={true}
            responsive
            striped
            highlightOnHover
            fixedHeader
            fixedHeaderScrollHeight="69vh"
            progressPending={this.state.isRecord}
            paginationComponentOptions={{ rowsPerPageText: `${this.props.t('ROWS_PER_PAGE')}` }}
            paginationPerPage={this.Defaultperpage()}
            paginationRowsPerPageOptions={this.perpage()}
          />
          </div>
        )}
      </div>
    );
  }
}

const ComponentTranslated = withTranslation()(MasterData);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
