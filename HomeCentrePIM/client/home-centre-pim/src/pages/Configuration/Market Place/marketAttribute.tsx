import {
  ComboBox,
  DefaultButton,
  Dropdown,
  Icon,
  IconButton,
  Modal,
  ProgressIndicator,
  SearchBox,
  Stack,
  TextField,
} from '@fluentui/react';
import React, { Suspense } from 'react';
import DataTable from 'react-data-table-component';
import { Text } from '@fluentui/react/lib/Text';
import { cancelIcon, iconButtonStyles } from '@/common/properties';
import { Grid } from '@mui/material';
import { Link } from 'react-router-dom';
import {
  PIMButtons,
  PIMcontentStyles,
  PIMsearchBoxStyles,
  columnHeader,
  PIMfooterButtonCancel,
  PIMfooterButtonConfirm,
  PIMComboBoxStyles,
  PIMHearderText,
} from '../../PimStyles';
import { MarketplaceAttribute, UploadMarketplaceAttributeModel } from '@/services/ApiClient';
import ApiManager from '@/services/ApiManager';
import Util from '@/services/Util';
import { withTranslation } from 'react-i18next';
import NotificationManager from '@/services/NotificationManager';
import UploadAttribute from './UploadAttributeXl';
import { BlockBlobClient } from '@azure/storage-blob';
import EditMarketPlaceAttribute from './EditMarketPlaceAttribute';
import { baseUrl } from '@/services/Constants';
import { downloadFileMarketplace } from '@/pages/commonFunctions';

class MarketAttribute extends React.Component<any, any> {
  apiClient = new ApiManager().CreateApiClient();
  notify = new NotificationManager();
  util = new Util();
  fieldType: any = [
    { key: true, text: this.props.t('MANDATORY') },
    { key: false, text: this.props.t('OPTIONAL') },
  ];

  constructor(props: any) {
    super(props);

    this.state = {
      marketAttributeData: [],
      tableData: [],
      selectionCount: 0,
      selectedRowData: [],
      searchData: [],
      noSelection: false,
      isRecord: false,
      templateName: '',
      attributeName: '',
      attributeDiscription: '',
      attributeDataType: null,
      fieldType: 0,
      rowDiscription: '',
      rowName: '',
      marketPlaceId: 0,
      isAddButton: false,
      divisionId: 4,
      divisionOpt: [],
      EditVisible: false,
      ProgressVisible: false,
      progressPercent: 0,
      dataType: [],
    };
  }

  componentDidMount(): void {
    this.refresh(this.state.divisionId);
  }

  async refresh(divId: any) {
    this.setState({ isRecord: true });
    const Ids: any = this.util.getUrlParameterId();
    const id: any = Ids.split(',');
    const marketPlaceId: any = parseInt(id[0]);
    const templateName: any = decodeURI(id[1]);
    var divOpt: any = [];
    const div = (await this.apiClient.getDivision()).result;
    const appConfiguration: any = await this.apiClient.getAppConfig();
    const dataType = JSON.parse(
      appConfiguration?.result?.find((e: any) => e.name == 'DataType')?.jsonValue,
    );
    const response = await this.apiClient.searchMarketplaceAttributes(
      '',
      0,
      0,
      marketPlaceId,
      divId,
    );
    div?.forEach((element: any) => {
      divOpt.push({ key: element.id, text: element.name });
    });
    this.setState({
      marketAttributeData: response.result?.records,
      tableData: response.result?.records,
      attriSearch: response.result?.records,
      divisionOpt: divOpt,
      marketPlaceId: marketPlaceId,
      noSelection: !this.state.noSelection,
      templateName: templateName,
      selectedRowData: [],
      selectionCount: 0,
      isRecord: false,
      attributeDataType: null,
      attributeDiscription: '',
      attributeName: '',
      dataType: dataType,
    });
    this.setState({
      marketAttributeData: response.result?.records?.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      })),
      searchData: response.result?.records?.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      })),
      tableData: response.result?.records?.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      })),
    });
  }

  rowSelection = (e: any) => {
    this.setState({ selectedRowData: e.selectedRows, selectionCount: e.selectedCount });
  };

  onChange = (e: any) => {
    const attributeList: any = [];
    for (var i = 0; i < e?.length; i++) {
      attributeList.push({
        key: e[i].key,
      });
    }
    this.setState({ attribute: attributeList, attributeName: '' });
  };
  perpage = () => {
    const recordCont = [25, 50, 100, 150, 200];
    return recordCont;
  };
  Defaultperpage = () => {
    const recordCont = 25;
    return recordCont;
  };
  async onUpload(files: any) {
    try {
      if (!files) {
        return new Promise(() => {});
      }
      var f: any = files[0];

      this.setState({ uploadVisible: false, ProgressVisible: true, percentageComplete: f.name });
      const uri = (await this.apiClient.getAuthorizedUrlForWrite(f?.name || ''))?.result;
      var uploadData = new UploadMarketplaceAttributeModel();
      uploadData.blobUrl = uri;
      uploadData.marketplaceId = this.state.marketPlaceId;
      uploadData.divisionId = this.state.divisionId;
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
          this.apiClient.processUploadedMarketplaceAttributes(uploadData).then((r) => {
            if (r.hasError) {
              this.notify.showErrorNotify(
                r.message || this.props.t('MSG-DATA-FILEUPLOAD-COMMON-ERROR'),
              );
            } else {
              this.notify.showSuccessNotify(this.props.t('MSG-DATA-FILEUPLOAD-COMMON-SUCCESS'));
              this.refresh(this.state.divisionId);
            }
          });
        });
    } catch (error) {
      this.setState({ ProgressVisible: false });
      this.notify.showErrorNotify(this.props.t('MSG-DATA-FILEUPLOAD-COMMON-ERROR'));
    }
  }
  onSubmit = async () => {
    var marketAttrList = new MarketplaceAttribute();
    marketAttrList.isActive = true;
    marketAttrList.marketplaceId = this.state.marketPlaceId;
    marketAttrList.name = this.state.attributeName;
    marketAttrList.description = this.state.attributeDiscription;
    marketAttrList.dataType = this.state.attributeDataType;
    marketAttrList.divisionId = this.state.divisionId;
    marketAttrList.isRequired = this.state.fieldType == 0 ? true : false;
    this.setState({ TemplateAttribute: false });
    try {
      this.apiClient.addOrUpdateMarketplaceAttribute(marketAttrList).then((r: any) => {
        let message =
          r.hasError == false ? `${this.props.t('MSG_ADD')}` : `${this.props.t('ERROR_UNKNOWN')}`;
        if (r.hasError) {
          this.notify.showCustomNotify(r.message);
        } else {
          this.notify.showSuccessNotify(message);
        }
        const removeData = this.state.marketAttributeData;
        const keysToRemove = ['isAdd'];
        for (let obj of removeData) {
          for (let key of keysToRemove) {
            delete obj[key];
          }
        }
        this.emptyString(removeData);
        this.refresh(this.state.divisionId);
        this.setState({
          visible: this.state.isAddButton,
          attributeName: '',
          attributeDataType: null,
          attributeDiscription: '',
          fieldType: 0,
        });
      });
    } catch (e: any) {
      this.notify.showErrorNotify(e);
    }
  };
  emptyString(value: any) {
    const filteredList = value.filter((obj: any) => Object.keys(obj).length !== 0);
    this.setState({ marketAttributeData: filteredList, isAddDisable: false });
  }
  downloadAttriTemplate = async () => {
    try {
      const url = baseUrl + `/api/Marketplace/DownloadMarketplaceAttributeTemplate`;
      downloadFileMarketplace(url);
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
  };
  onRemove = async () => {
    try {
      const selectedIds = [];
      for (let i = 0; i < this.state.selectedRowData.length; i++) {
        selectedIds.push(this.state.selectedRowData[i]?.id);
      }
      const response = await this.apiClient.removeMarketplaceAttributes(selectedIds);
      let message =
        response.hasError == false
          ? `${this.props.t('MSG_REMOVE')}`
          : `${this.props.t('ERROR_UNKNOWN')}`;
      this.setState({ confirmModel: false });
      if (response.hasError) {
        this.notify.showErrorNotify(message);
      } else {
        this.notify.showSuccessNotify(message);
      }
      this.refresh(this.state.divisionId);
    } catch (e: any) {
      this.notify.showErrorNotify(e);
    }
  };

  onSearch(e: any) {
    var newValue = e?.target.value;
    newValue = newValue == undefined ? '' : newValue;
    this.setState({ searchKey: newValue });
    var result = this.state.searchData.filter((element: any) => {
      return element?.name
        ?.toLowerCase()
        .includes(newValue.trim().split(/ +/).join(' ').toLowerCase());
    });
    this.setState({ tableData: result });
    this.setState({
      tableData: result?.map((item: any, index: any) => ({ index: index + 1, ...item })),
    });
  }

  onAttributeSearch(e: any) {
    var newAttriValue = e?.target.value;
    newAttriValue = newAttriValue == undefined ? '' : newAttriValue;
    this.setState({ attriSearch: newAttriValue });
    var attriResult = this.state.attriSearch.filter((element: any) => {
      return element?.name
        ?.toLowerCase()
        .includes(newAttriValue.trim().split(/ +/).join(' ').toLowerCase());
    });
    this.setState({ attributeData: attriResult });
    this.setState({
      attributeData: attriResult?.map((itemOne: any, indexOne: any) => ({
        indexOne: indexOne + 1,
        ...itemOne,
      })),
    });
  }
  DeleteMarketPlace = async () => {
    try {
      if (this.state.selectedRowData && this.state.selectedRowData.length > 0) {
        const ids = this.state.selectedRowData.map((e: any) => parseInt(e.id, 10));
        await this.apiClient.deleteMarketPlace(ids);
        this.notify.showSuccessNotify('Deleted successfully');
        this.refresh(this.state.divisionId);
      }
    } catch (error) {
      console.error('Error deleting user:', error);
    }
  };

  render() {
    var { attributeName, attributeDataType, attributeDiscription } = this.state;
    const { t } = this.props;
    const columns = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{`${t('S_NO')}`}</div>,
        selector: (row: any) => row.index,
        isVisible: true,
      },
      {
        key: 'attributes',
        name: <div className={columnHeader}>{`${t('ATTRIBUTES')}`}</div>,
        selector: (row: any) => row.name,
        isVisible: true,
      },
      {
        key: 'attributeDiscription',
        name: <div className={columnHeader}>{`${t('ATTRIBUTE_DISCRIPTION')}`}</div>,
        selector: (row: any) => row.description,
        isVisible: true,
      },
      {
        key: 'dataType',
        name: <div className={columnHeader}>{`${t('DATA_TYPE')}`}</div>,
        selector: (row: any) => this.state.dataType.find((e: any) => e.key == row.dataType)?.text,
        isVisible: true,
      },
      {
        key: 'fieldType',
        name: <div className={columnHeader}>{`${t('FIELD_TYPE')}`}</div>,
        selector: (row: any) =>
          row.isRequired == true
            ? `${this.props.t('MANDATORY')}`
            : row.isRequired == false
            ? `${this.props.t('OPTIONAL')}`
            : '',
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
    const addColumns: any = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{`${t('S_NO')}`}</div>,
        selector: (row: any) => row.index,
        width: '70px',
      },
      {
        key: 'attributes',
        name: <div className={columnHeader}>{`${t('ATTRIBUTES')}`}</div>,
        selector: (row: any) => (
          <TextField
            className="textField"
            value={!row.isAdd ? row.name : this.state.attributeName}
            readOnly={!row.isAdd}
            onChange={(e: any) => this.setState({ attributeName: e.target.value })}
          />
        ),
        minWidth: '170px',
      },
      {
        key: 'attributeDiscription',
        name: <div className={columnHeader}>{`${t('ATTRIBUTE_DISCRIPTION')}`}</div>,
        selector: (row: any) => (
          <TextField
            className="textField"
            value={!row.isAdd ? row.description : this.state.attributeDiscription}
            readOnly={!row.isAdd}
            onChange={(e: any) => this.setState({ attributeDiscription: e.target.value })}
          />
        ),
        minWidth: '170px',
      },
      {
        key: 'dataType',
        name: <div className={columnHeader}>{`${t('DATA_TYPE')}`}</div>,
        selector: (row: any) => (
          <div>
            {' '}
            {row.isAdd ? (
              <ComboBox
                options={this.state.dataType}
                placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                defaultSelectedKey={row.dataType}
                selectedKey={row.dataType}
                onChange={(e: any, selectedOption: any) => {
                  this.setState({ attributeDataType: selectedOption.key });
                }}
                styles={PIMComboBoxStyles}
              />
            ) : (
              <ComboBox options={this.state.dataType} selectedKey={row.dataType} />
            )}
          </div>
        ),
        minWidth: '180px',
      },
      {
        key: 'fieldType',
        name: <div className={columnHeader}>{`${t('FIELD_TYPE')}`}</div>,
        selector: (row: any) => (
          <div>
            <Grid lg={12} item container direction={'row'}>
              <Grid item lg={row.isAdd ? 10 : 12} xs={10}>
                <ComboBox
                  selectedKey={!row.isAdd ? (row.isRequired ? 0 : 1) : this.state.fieldType}
                  options={this.fieldType}
                  defaultSelectedKey={0}
                  onChange={(e: any, selectedOption: any) =>
                    this.setState({ fieldType: selectedOption.key })
                  }
                />
              </Grid>
              {row.isAdd && (
                <Grid item lg={2} xs={2}>
                  <IconButton
                    style={{
                      color: 'white',
                      backgroundColor: 'red',
                      marginTop: '0px',
                      width: '100%',
                    }}
                    styles={iconButtonStyles}
                    iconProps={cancelIcon}
                    ariaLabel="Close popup modal"
                    onClick={() => {
                      const removeData = this.state.marketAttributeData;
                      const keysToRemove = ['isAdd'];
                      for (let obj of removeData) {
                        for (let key of keysToRemove) {
                          delete obj[key];
                        }
                      }
                      this.emptyString(removeData);
                      this.setState({
                        attributeDiscription: '',
                        attributeDataType: null,
                        attributeName: '',
                        fieldType: 0,
                      });
                    }}
                  />
                </Grid>
              )}
            </Grid>
          </div>
        ),
        width: '180px',
      },
    ];
    const handleAddColumnReorder = (newColumns: any) => {
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
          {/* <Text block className="text">{`${t('PIM')} /`}<Link to='/pim/marketplace'>{`${t('MARKET_PLACE')} `}</Link></Text> */}
          <Text variant="xxLarge" styles={PIMHearderText} className="color-blue text">
            {this.state.templateName}
          </Text>
        </Stack>
        <Grid lg={12} item container spacing={2} direction={'row'}>
          <Grid item lg={2} xs={12}>
            <SearchBox
              className="searchBox"
              styles={PIMsearchBoxStyles}
              placeholder={`${t('ATTRIBUTE_SEARCH')}`}
              onChange={(e: any) => this.onSearch(e)}
              value={this.state.searchKey}
              onClear={() => this.setState({ searchKey: '' })}
            />
          </Grid>
          <Grid item lg={1.6} xs={6}>
            <DefaultButton
              className="button"
              iconProps={{ iconName: 'CircleAddition' }}
              styles={PIMButtons}
              text={`${t('BTN_ADD_ATTRIBUTE')}`}
              onClick={() => {
                this.setState({ visible: true, isAdd: false });
              }}
              disabled={this.state.selectionCount < 1 ? false : true}
            />
          </Grid>
          <Grid item lg={1.3} xs={6}>
            <DefaultButton
              className="button"
              iconProps={{ iconName: 'Delete' }}
              styles={PIMButtons}
              text={`${t('BTN_DEL')}`}
              onClick={() => this.setState({ confirmModel: true })}
              disabled={this.state.selectionCount > 0 ? false : true}
            />
          </Grid>
          <Grid item lg={1} xs={6}>
            <DefaultButton
              className="button"
              iconProps={{ iconName: 'Edit' }}
              styles={PIMButtons}
              text={`${t('BTN_EDIT')}`}
              onClick={() => this.setState({ EditVisible: true })}
              disabled={this.state.selectionCount == 1 ? false : true}
            />
          </Grid>
          <Grid item lg={1.5} xs={6}>
            <DefaultButton
              className="button"
              iconProps={{ iconName: 'Download' }}
              text={t('BTN_DOWNLOAD')}
              styles={PIMButtons}
              onClick={this.downloadAttriTemplate.bind(this)}
            />
          </Grid>
          <Grid item lg={1.3} xs={6}>
            <DefaultButton
              className="button"
              iconProps={{ iconName: 'Upload' }}
              styles={PIMButtons}
              text={`${t('BTN_UPLOAD_ATTRIBUTE')}`}
              disabled={this.state.selectionCount != 1 ? false : true}
              onClick={() => this.setState({ uploadVisible: true })}
            />
          </Grid>
          <Grid item lg={1.5} xs={6}>
            <Dropdown
              className="dropdown"
              options={this.state.divisionOpt}
              defaultSelectedKey={this.state.divisionId}
              onChange={(e: any, selectedOption: any) => {
                this.setState({ divisionId: selectedOption.key });
                this.refresh(selectedOption.key);
              }}
            />
          </Grid>
          <Grid item lg={1.3} />
          <Grid item lg={0.5} xs={12}>
            <Icon
              iconName="Refresh"
              title={t('BTN_REFRESH')}
              className="iconStyle"
              onClick={() => {
                this.refresh(this.state.divisionId);
              }}
            />
          </Grid>
        </Grid>
        <Modal
          isOpen={this.state.EditVisible}
          containerClassName={PIMcontentStyles.attributeContainer}
          onDismiss={() =>
            this.setState({
              EditVisible: false,
            })
          }
        >
          <div className={PIMcontentStyles.header}>
            <div className="apptext1">{`${t('EDIT_MARKETPLACE_ATTRIBUTE')}`}</div>
            {/* <IconButton
                  styles={iconButtonStyles}
                  iconProps={cancelIcon}
                  ariaLabel="Close popup modal"
                  onClick={() => {
                    this.setState({ EditVisible: false });
                  }}
                /> */}
          </div>
          <div className={PIMcontentStyles.body}>
            <EditMarketPlaceAttribute
              t={t}
              fields={this.state.selectedRowData}
              DataTypeOption={this.state.dataType}
              reload={() => this.refresh(this.state.divisionId)}
              DivisionId={this.state.divisionId}
              ClosePopup={() => {
                this.setState({ EditVisible: false });
                this.refresh(this.state.divisionId);
              }}
              CloseOnly={() => this.setState({ EditVisible: false })}
            />
          </div>
        </Modal>
        <Modal
          isOpen={this.state.uploadVisible}
          containerClassName={PIMcontentStyles.ExcelContainer}
          onDismiss={() =>
            this.setState({
              uploadVisible: false,
            })
          }
        >
          <div className={PIMcontentStyles.header}>
            <div className="apptext1">
              <h2>{t('UPLOAD')}</h2>
            </div>
            {/* <IconButton
                  styles={iconButtonStyles}
                  iconProps={cancelIcon}
                  ariaLabel="Close popup modal"
                  onClick={() => {
                    this.setState({ uploadVisible: false });
                  }}
                /> */}
          </div>
          <div className={PIMcontentStyles.body}>
            <UploadAttribute
              ClosePopup={() => this.setState({ uploadVisible: false })}
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
          <div style={{ marginTop: '7px' }}>
            <Text variant="small" className="text">
              <b>{'Uploading...'}</b>
            </Text>
          </div>
          <ProgressIndicator barHeight={20} percentComplete={this.state.progressPercent} />
        </Modal>
        <Modal
          isOpen={this.state.confirmModel}
          containerClassName={PIMcontentStyles.confirmContainer}
          onDismiss={() =>
            this.setState({
              confirmModel: false,
            })
          }
        >
          <div className={PIMcontentStyles.confirmHeader}>
            {/* <IconButton
                  styles={iconButtonStyles}
                  iconProps={cancelIcon}
                  ariaLabel="Close popup modal"
                  onClick={() => {
                    this.setState({ confirmModel: false });
                  }}
                /> */}
          </div>
          <div className={PIMcontentStyles.confirmbody}>
            <Text variant="xLarge" className="apptext1">{`${t('MSG_CONFIRM')}`}</Text>
          </div>
          <div className={PIMcontentStyles.footer}>
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={1.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonConfirm}
                  text={`${t('BTN_CONFIRM')}`}
                  onClick={this.onRemove.bind(this)}
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
              <Grid item lg={1.5} xs={12} />
            </Grid>
          </div>
        </Modal>
        <Modal
          isOpen={this.state.visible}
          containerClassName={PIMcontentStyles.tableContainer}
          isBlocking={true}
          onDismiss={() =>
            this.setState({
              visible: false,
            })
          }
        >
          <div className={PIMcontentStyles.header}>
            <span className="apptext1">
              {this.state.templateName} - {t('ADD_ATTRIBUTE')}
            </span>
            {/* <IconButton
                  styles={iconButtonStyles}
                  iconProps={cancelIcon}
                  ariaLabel="Close popup modal"
                  onClick={() => {
                    var data = this.state;
                    this.setState({
                      attributeDiscription: '',
                      attributeDataType: null,
                      attributeName: '',
                      marketAttributeData: data.marketAttributeData.filter(
                        (element: any) => !element.isAdd == true,
                      ),
                      isAddDisable: false,
                      visible: false,
                      noSelection: !this.state.noSelection,
                      selectedRowData: [],
                      selectionCount: 0,
                      fieldType: 0,
                    });
                  }}
                /> */}
          </div>
          <div className={PIMcontentStyles.body}>
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={12} xs={12}>
                <div className="dataTableFixedFooter">
                  <DataTable
                    columns={addColumns}
                    data={this.state.marketAttributeData}
                    pagination={true}
                    onColumnOrderChange={handleAddColumnReorder}
                    selectableRows={this.state.selectedRowData}
                    onSelectedRowsChange={this.rowSelection}
                    clearSelectedRows={this.state.noSelection}
                    selectableRowsHighlight
                    highlightOnHover
                    responsive
                    fixedHeader
                    striped
                    paginationComponentOptions={{
                      rowsPerPageText: `${this.props.t('ROWS_PER_PAGE')}`,
                    }}
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
                <Grid lg={12} item container spacing={2} direction={'row'}>
                  <Grid item lg={1.5} xs={5}>
                    <DefaultButton
                      className="button"
                      text="add"
                      iconProps={{ iconName: 'CircleAddition' }}
                      styles={PIMButtons}
                      onClick={() => {
                        this.setState({
                          marketAttributeData: this.state.marketAttributeData.concat({
                            isAdd: true,
                          }),
                          isAddDisable: true,
                        });
                      }}
                      disabled={this.state.isAddDisable}
                    />
                  </Grid>
                </Grid>
              </Grid>
            </Grid>
          </div>
          <div className={PIMcontentStyles.footer}>
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={1.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  text={t('BTN_SAVE')}
                  disabled={
                    this.state.marketAttributeData[this.state.marketAttributeData?.length - 1]
                      ?.isAdd != undefined &&
                    this.state.marketAttributeData[this.state.marketAttributeData.length - 1]?.isAdd
                      ? !(attributeName != '') ||
                        !(attributeName.trim() != '') ||
                        !(attributeDataType != null)
                      : !this.state.isAddDisable
                  }
                  onClick={() => {
                    this.setState({ isAddButton: true });
                    this.onSubmit();
                  }}
                  styles={PIMfooterButtonConfirm}
                />
              </Grid>
              <Grid item lg={1} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonConfirm}
                  text={`${t('BTN_SAVE_CLOSE')}`}
                  disabled={
                    this.state.marketAttributeData[this.state.marketAttributeData.length - 1]
                      ?.isAdd != undefined &&
                    this.state.marketAttributeData[this.state.marketAttributeData.length - 1]?.isAdd
                      ? !(attributeName != '') ||
                        !(attributeName.trim() != '') ||
                        !(attributeDataType != null)
                      : !this.state.isAddDisable
                  }
                  onClick={() => {
                    this.setState({ isAddButton: false });
                    this.onSubmit();
                  }}
                />
              </Grid>
              <Grid item lg={1.5} xs={12} />
            </Grid>
          </div>
        </Modal>
        <div className="dataTableFixedFooter">
          <DataTable
            columns={columns}
            data={this.state.tableData}
            pagination={true}
            onColumnOrderChange={handleColumnReorder}
            selectableRows={this.state.selectedRowData}
            onSelectedRowsChange={this.rowSelection}
            clearSelectedRows={this.state.noSelection}
            selectableRowsHighlight
            highlightOnHover
            responsive
            fixedHeader
            striped
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
    );
  }
}

const ComponentTranslated = withTranslation()(MarketAttribute);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
