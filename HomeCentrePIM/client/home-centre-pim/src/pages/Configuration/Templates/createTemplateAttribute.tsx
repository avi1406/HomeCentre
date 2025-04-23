import { DefaultButton, Icon, IconButton, Modal, SearchBox, Stack } from '@fluentui/react';
import React, { Suspense } from 'react';
import DataTable from 'react-data-table-component';
import { Text } from '@fluentui/react/lib/Text';
import {
  AttributeStyles,
  CancelButtonStyles,
  cancelIcon,
  iconButtonStyles,
  UploadStyles,
} from '@/common/properties';
import { Grid } from '@mui/material';
import { Link } from 'react-router-dom';
import {
  PIMattributeSelect,
  PIMButtons,
  PIMcontentStyles,
  PIMsearchBoxStyles,
  columnHeader,
  PIMfooterButtonCancel,
  PIMfooterButtonConfirm,
} from '../../PimStyles';
import { TemplateAttribute } from '@/services/ApiClient';
import ApiManager from '@/services/ApiManager';
import Util from '@/services/Util';
import { withTranslation } from 'react-i18next';
import NotificationManager from '@/services/NotificationManager';

class CreateTemplateAttribute extends React.Component<any, any> {
  apiClient = new ApiManager().CreateApiClient();
  notify = new NotificationManager();
  util = new Util();
  constructor(props: any) {
    super(props);

    this.state = {
      selectionCount: 0,
      selectedRowData: [],
      searchData: [],
      noSelection: false,
      visible: false,
      Template: false,
      templateAttributeData: [],
      attributeList: [],
      isRecord: false,
      attribute: 0,
      errors: {},
      templateId: 0,
      attributeName: '',
      msgVisible: false,
      message: '',
      messageType: false,
      confirmModel: false,
      attributeData: [],
      attriSearch: [],
      divisionOptionlist: [],
      templateName: '',
      searchKey: '',
      dataType: [],
    };
  }

  async componentDidMount() {
    this.setState({ isRecord: true });
    const appConfiguration: any = await this.apiClient.getAppConfig();
    const dataType = JSON.parse(
      appConfiguration?.result?.find((e: any) => e.name == 'DataType')?.jsonValue,
    );
    this.apiClient.getDivision().then((r: any) => {
      var division: any = [];
      r.result?.forEach((element: any) => {
        division.push({
          key: element.id,
          text: element.name,
        });
      });
      this.setState({ divisionOptionlist: division, dataType: dataType });
    });
    this.refresh();
  }

  async refresh() {
    this.setState({ isRecord: true });
    const Ids: any = this.util.getUrlParameterId();
    const id: any = Ids.split(',');
    const templateId: any = parseInt(id[0]);
    const divisionId: any = parseInt(id[1]);
    const templateName: any = decodeURI(id[2]);
    const attributeResponse = await this.apiClient.listAttributes(divisionId, true);
    const response = await this.apiClient.listTemplateAttributes(templateId);
    var filteredArray = attributeResponse.result?.filter(function (attribute: any) {
      return (
        response.result?.filter(function (tempAttri: any) {
          return tempAttri.attributeId == attribute.id;
        }).length == 0
      );
    });
    var filteredAttribute = filteredArray?.filter((data: any) => {
      return data.divisionId == divisionId;
    });
    this.setState({
      attributeData: filteredAttribute,
      attriSearch: filteredArray,
      templateName: templateName,
      templateAttributeData: response.result,
      searchData: response.result,
      noSelection: !this.state.noSelection,
      searchKey: '',
      selectedRowData: [],
      selectionCount: 0,
      templateId: templateId,
      isRecord: false,
    });
    this.setState({
      attributeData: filteredAttribute?.map((itemOne: any, indexOne: any) => ({
        indexOne: indexOne + 1,
        ...itemOne,
      })),
      templateAttributeData: response.result?.map((item: any, index: any) => ({
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

  onSubmit = async () => {
    var tempAttrList: TemplateAttribute[] = [];
    for (var i = 0; i < this.state.selectedRowData.length; i++) {
      tempAttrList.push(
        new TemplateAttribute({
          attributeId: this.state.selectedRowData[i].id,
          templateId: this.state.templateId,
          isActive: true,
        }),
      );
    }
    this.setState({ visible: false, TemplateAttribute: false, addConfirm: false });
    try {
      this.apiClient.linkAttributesToTemplate(tempAttrList).then((r: any) => {
        let message =
          r.hasError == false ? `${this.props.t('MSG_ADD')}` : `${this.props.t('ERROR_UNKNOWN')}`;
        if (r.hasError) {
          this.notify.showErrorNotify(r.message);
        } else {
          this.notify.showSuccessNotify(message);
        }
        this.refresh();
      });
    } catch (e: any) {
      this.notify.showErrorNotify(e);
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
  onRemove = async () => {
    try {
      const selectedIds = [];
      for (let i = 0; i < this.state.selectedRowData.length; i++) {
        selectedIds.push(this.state.selectedRowData[i]?.templateAttributeId);
      }
      const response = await this.apiClient.removeAttributesFromTemplate(selectedIds);
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
      this.refresh();
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
    this.setState({ templateAttributeData: result });
    this.setState({
      templateAttributeData: result?.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      })),
    });
  }

  onAttributeSearch(e: any) {
    var newAttriValue = e?.target.value;
    newAttriValue = newAttriValue == undefined ? '' : newAttriValue;
    this.setState({ attriSearch: newAttriValue });
    var attriResult = this.state.attriSearch.filter((element: any) => {
      return element?.name?.toLowerCase().includes(newAttriValue.toLowerCase());
    });
    this.setState({ attributeData: attriResult });
    this.setState({
      attributeData: attriResult?.map((itemOne: any, indexOne: any) => ({
        indexOne: indexOne + 1,
        ...itemOne,
      })),
    });
  }

  render() {
    const { t } = this.props;
    const columns = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{`${t('S_NO')}`}</div>,
        selector: (row: any) => row.index || row.indexOne,
        isVisible: true,
      },

      {
        key: 'attributes',
        name: <div className={columnHeader}>{`${t('ATTRIBUTES')}`}</div>,
        selector: (row: any) => <span title={row.name}>{row.name}</span>,
        isVisible: true,
        minwidth: '500px',
      },
      {
        key: 'datatype',
        name: <div className={columnHeader}>{`${t('DATA_TYPE')}`}</div>,
        selector: (row: any) => (
          <span title={this.state.dataType?.find((e: any) => e.key == row.dataType)?.text}>
            {this.state.dataType?.find((e: any) => e.key == row.dataType)?.text}
          </span>
        ),
        isVisible: true,
        minwidth: '500px',
      },
      {
        key: 'datatype',
        name: <div className={columnHeader}>{`${t('DATA_TYPE')}`}</div>,
        selector: (row: any) => this.state.dataType?.find((e: any) => e.key == row.dataType)?.text,
        isVisible: false,
        minwidth: '500px',
      },
      {
        key: 'division',
        name: <div className={columnHeader}>{`${t('DIVISION')}`}</div>,
        selector: (row: any) => (
          <span
            title={this.state.divisionOptionlist?.find((e: any) => e.key == row.divisionId)?.text}
          >
            {this.state.divisionOptionlist?.find((e: any) => e.key == row.divisionId)?.text}
          </span>
        ),
        isVisible: this.state.visible == true ? true : false,
      },
      {
        key: 'fieldtype',
        name: <div className={columnHeader}>{`${t('FIELD_TYPE')}`}</div>,
        selector: (row: any) => (
          <span title={row.isRequired ? this.props.t('MANDATORY') : this.props.t('OPTIONAL')}>
            {row.isRequired ? this.props.t('MANDATORY') : this.props.t('OPTIONAL')}
          </span>
        ),
        isVisible: this.state.visible == true ? true : false,
        minwidth: '500px',
      },
    ]?.filter((item) => item.isVisible);
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
          {/* <Text block className="text">{`${t('PIM')} /`}<Link to='/pim/template'>{`${t('TEMPLATES')} `}</Link>{`/ ${this.state.templateName}`}</Text> */}
          <Text variant="xxLarge" className="color-blue text">
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
          <Grid item lg={1.3} xs={12}>
            <span title={t('ROLE_ADD_BTN')}>
              <Icon
                iconName="CircleAddition"
                className="buttonIcon"
                onClick={() => {
                  if (this.state.selectionCount < 1) {
                    this.setState({ visible: true });
                  }
                }}
                style={{
                  cursor: this.state.selectionCount < 1 ? 'pointer' : 'not-allowed',
                  color: this.state.selectionCount < 1 ? 'inherit' : 'gray',
                }}
              />
            </span>
          </Grid>
          <Grid item lg={1.5} xs={12}>
            <span title={t('BTN_DEL')}>
              <Icon
                iconName="Delete"
                className="buttonIcon1"
                onClick={() => {
                  if (this.state.selectionCount > 0) {
                    this.setState({ confirmModel: true });
                  }
                }}
                style={{
                  cursor: this.state.selectionCount > 0 ? 'pointer' : 'not-allowed',
                  color: this.state.selectionCount > 0 ? 'inherit' : 'gray',
                }}
              />
            </span>
          </Grid>
          <Grid item lg={2} xs={12}>
            {this.state.selectionCount > 0 && (
              <DefaultButton
                className="button rowbtn"
                text={`${this.props.t('ROW_COUNT')} | ${this.state.selectionCount}`}
                styles={PIMattributeSelect}
              />
            )}
          </Grid>
          <Grid item lg={4.6} xs={12} />
          <Grid item lg={0.5} xs={12}>
            {/* <DefaultButton
                            className='button'
                            iconProps={{ iconName: 'Refresh' }}
                            text={t('BTN_REFRESH')}
                            styles={PIMButtons}
                            onClick={() => this.refresh()}
                        /> */}
            <Icon
              iconName="Refresh"
              title={this.props.t('BTN_REFRESH')}
              className="iconStyle"
              onClick={() => this.refresh()}
            />
          </Grid>
        </Grid>
        <Modal
          isOpen={this.state.confirmModel}
          containerClassName={PIMcontentStyles.confirmContaineruser1}
          isBlocking={false}
          onDismiss={() =>
            this.setState({
              confirmModel: false,
            })
          }
        >
          <div className={PIMcontentStyles.confirmHeaderUser1}>
              <IconButton
                styles={UploadStyles}
                iconProps={cancelIcon}
                className='cancelbtn'
                ariaLabel="Close popup modal"
                onClick={() => {
                  this.setState({
                    confirmModel: false,
                    noSelection: !this.state.noSelection,
                    selectedRowData: [],
                    selectionCount: 0,
                  });
                }}
              />
            </div>
          <div className={PIMcontentStyles.confirmbody4}>
            <Text variant="xLarge" className="apptext2 dlttxt">{`${t('MSG_CONFIRM')}`}</Text>
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
              <Grid item lg={0.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonCancel}
                  text={`${t('BTN_CANCEL')}`}
                  onClick={() => {
                    this.setState({ 
                      confirmModel: false,
                      noSelection: !this.state.noSelection,
                      selectedRowData: [],
                      selectionCount: 0,
                    });
                  }}
                />
              </Grid>
                <Grid item lg={2} xs={12} />
            </Grid>
          </div>
        </Modal>
        <Modal
          isOpen={this.state.visible}
          containerClassName={PIMcontentStyles.tableContainer1}
          // isBlocking={true}
          onDismiss={() =>
            this.setState({
              visible: false,
              noSelection: !this.state.noSelection,
              selectedRowData: [],
              selectionCount: 0,
            })
          }
        >
          <div className={PIMcontentStyles.header}>
            <span className="apptext1">
              {this.state.templateName} - {t('ADD_ATTRIBUTE')}
            </span>
            <IconButton
              styles={AttributeStyles}
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
            />
          </div>
          <div className={PIMcontentStyles.body} style={{ marginBottom: '0%' }}>
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={12} xs={12}>
                <div className="layout width-100">
                  <Modal
                    isOpen={this.state.addConfirm}
                    containerClassName={PIMcontentStyles.confirmContainer}
                    // isBlocking={true}
                    onDismiss={() =>
                      this.setState({
                        addConfirm: false,
                      })
                    }
                  >
                    {/* <div className={PIMcontentStyles.confirmHeader}>
                      <IconButton
                        styles={iconButtonStyles}
                        iconProps={cancelIcon}
                        ariaLabel="Close popup modal"
                        onClick={() => {
                          this.setState({ addConfirm: false });
                        }}
                      />
                    </div> */}
                    <div className={PIMcontentStyles.confirmbody}>
                      {`${this.props.t('TEMPLATE_ATTRIBUTE_ADD_CONFIRM')}`}
                    </div>
                    <div className={PIMcontentStyles.footer}>
                      <Grid lg={12} item container spacing={2} direction={'row'}>
                        <Grid item lg={1} xs={12} />
                        <Grid item lg={4} xs={12}>
                          <DefaultButton
                            className="button"
                            onClick={this.onSubmit.bind(this)}
                            styles={PIMfooterButtonConfirm}
                            text={`${this.props.t('BTN_CONFIRM')}`}
                          />
                        </Grid>
                        <Grid item lg={2} xs={12} />
                        <Grid item lg={4} xs={12}>
                          <DefaultButton
                            className="button"
                            styles={PIMfooterButtonCancel}
                            onClick={() => {
                              this.setState({ addConfirm: false });
                            }}
                            text={`${this.props.t('BTN_CANCEL')}`}
                          />
                        </Grid>
                        <Grid item lg={1} xs={12} />
                      </Grid>
                    </div>
                  </Modal>
                  <Grid lg={12} item container spacing={2} direction={'row'}>
                    <Grid item lg={4} xs={12}>
                      {this.state.selectionCount > 0 && (
                        <DefaultButton
                          className="button"
                          text={`${t('ROW_COUNT')} | ${this.state.selectionCount}`}
                          styles={PIMattributeSelect}
                        />
                      )}
                    </Grid>
                  </Grid>
                  <DataTable
                    columns={columns}
                    data={this.state.attributeData}
                    pagination={true}
                    onColumnOrderChange={handleColumnReorder}
                    selectableRows={this.state.selectedRowData}
                    onSelectedRowsChange={this.rowSelection}
                    clearSelectedRows={this.state.noSelection}
                    selectableRowsHighlight
                    highlightOnHover
                    fixedHeaderScrollHeight="66vh"
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
              </Grid>
            </Grid>
          </div>
          <div className={PIMcontentStyles.footer}>
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={1.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonConfirm}
                  text={`${this.props.t('BTN_CONFIRM')}`}
                  disabled={this.state.selectionCount >= 1 ? false : true}
                  onClick={() => this.setState({ addConfirm: true })}
                />
              </Grid>
              <Grid item lg={1} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonCancel}
                  text={`${this.props.t('BTN_CANCEL')}`}
                  onClick={() => {
                    this.setState({
                      visible: false,
                      noSelection: !this.state.noSelection,
                      selectedRowData: [],
                      selectionCount: 0,
                    });
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
            data={this.state.templateAttributeData}
            pagination={true}
            onColumnOrderChange={handleColumnReorder}
            selectableRows={this.state.selectedRowData}
            onSelectedRowsChange={this.rowSelection}
            clearSelectedRows={this.state.noSelection}
            selectableRowsHighlight
            highlightOnHover
            responsive
            fixedHeader
            fixedHeaderScrollHeight="68.2vh"
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

const ComponentTranslated = withTranslation()(CreateTemplateAttribute);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
