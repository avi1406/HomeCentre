import { DefaultButton, Dropdown, Icon, IconButton, IDropdownOption, ISelectableOption, Modal, SearchBox, Stack } from '@fluentui/react';
import { Grid } from '@mui/material';
import React, { Component, Suspense } from 'react';
import { Text } from '@fluentui/react/lib/Text';
import { CancelButtonStyles, cancelIcon } from '@/common/properties';
import DataTable from 'react-data-table-component';
import { Link } from "react-router-dom";
import { iconButtonStyles } from '@/common/Button';
import AddOrUpdateDataRequest from './AddOrUpdateDataRequest';
import { activeText, AttributeheadDropdown, boxStyles, columnHeader, PIMButtons, PIMcontentStyles, PIMdropdownStyles, PIMfooterButtonCancel, PIMfooterButtonConfirm, PIMHeaderButtons, PIMHearderText, PIMsearchBoxStyles } from '../../PimStyles';
import ApiManager from '@/services/ApiManager';
import { withTranslation } from 'react-i18next';
import jwt_decode from "jwt-decode";
import NotificationManager from '@/services/NotificationManager';
import { renderOption } from '@/pages/commonFunctions';

class DataRequest extends Component<any, any> {
  apiClient = new ApiManager().CreateApiClient();
  notify = new NotificationManager();
  options: IDropdownOption[] = [
    { key: 0, text: 'Yes' },
    { key: 1, text: 'No' },
  ];

  constructor(props: any) {
    super(props);
    this.state = {
      isRecord: false,
      dataSource: [],
      requestData: [],
      selectedRowData: [],
      noSelection: false,
      enableEditButton: false,
      searchKey: '',
      isActive: true,
      Deactivate: false,
      AddOrUpdate: false,
      templateDropdownOption: [],
      marketDropdownOption: [],
      userDivision: 0,
      confirmModel: false,
      marketId: 1,
      departmentOption: [],
      productID: 652,
    };
  }

  async componentDidMount() {
    this.setState({ isRecord: true });
    var marketOption: any = [];
    var deptOpt: any = [];
    const marketOpt = (await this.apiClient.searchMarketplaces('', 0, 0, true)).result?.records;
    marketOpt?.forEach((value: any) => {
      marketOption.push({
        key: value.id,
        text: value.name,
      });
    });
    const depyResponse = (await this.apiClient.getDepartments()).result;
    depyResponse?.forEach((e: any) => {
      deptOpt.push({ key: e.id, text: e.name });
    });
    const options = marketOption.filter((item: any) => item.key !== 1);
    this.setState({ marketDropdownOption: options, departmentOption: deptOpt });
    this.refresh();
  }

  async refresh(isActive?: boolean) {
    const isActives = isActive == undefined ? true : isActive;
    const data = (await this.apiClient.searchDataRequests('', 0, 0, isActives)).result?.records;
    this.setState({
      dataSource: data,
      selectedRowData: [],
      noSelection: !this.state.noSelection,
      Deactivate: false,
      searchKey: '',
    });
    this.templateOpt(this.state.marketId, this.state.productID);
  }
  perpage = () => {
    const recordCont = [25, 50, 100, 150, 200];
    return recordCont;
  };
  Defaultperpage = () => {
    const recordCont = 25;
    return recordCont;
  };
  async templateOpt(id?: any, productId?: any) {
    this.setState({ marketId: id, productID: productId });
    var option: any = [];
    const marketId = id == null ? 1 : id;
    const productDept = productId == null ? 652 : productId;
    const token = sessionStorage.getItem('token');
    const decode: any = jwt_decode(token || '');
    const divisionIds: any = decode?.DepartmentId.split(',');
    for (var i = 0; i < divisionIds.length; i++) {
      if (divisionIds[i] != 0) {
        this.setState({ userDivision: parseInt(divisionIds[i]) });
        const tempdrop = await this.apiClient.searchMarketplaceTemplates(
          '',
          0,
          0,
          marketId,
          parseInt(divisionIds[i]),
          productDept,
          true,
        );
        tempdrop.result?.records?.forEach((element: any) => {
          option.push({ key: element.id, text: element.name });
        });
        break;
      }
    }
    const data = this.state.dataSource.filter((element: any) => {
      return (
        option.filter((e: any) => {
          return element.templateId == e.key;
        }).length != 0
      );
    });
    this.setState({ templateDropdownOption: option, requestData: data, isRecord: false });
    this.setState({
      requestData: data?.map((item: any, index: any) => ({ index: index + 1, ...item })),
      searchData: data?.map((item: any, index: any) => ({ index: index + 1, ...item })),
    });
  }
  async activeState() {
    try {
      const selectedRecordIds = [];
      for (let i = 0; i < this.state.selectedRowData.length; i++) {
        selectedRecordIds.push(this.state.selectedRowData[i].id);
      }
      const response = !this.state.isActive
        ? await this.apiClient.deactivateDataRequests(selectedRecordIds)
        : await this.apiClient.activateDataRequests(selectedRecordIds);
      let message =
        response.hasError == true
          ? `${this.props.t('ERROR_UNKNOWN')}`
          : this.state.isActive
          ? `${this.props.t('MSG_DEACTIVATE')}`
          : `${this.props.t('MSG_ACTIVATE')}`;
      this.setState({ confirmModel: false });
      this.notify.showSuccessNotify(message);
      this.refresh(this.state.isActive);
    } catch (e: any) {
      this.notify.showErrorNotify(e);
    }
  }
  rowSelection = (Event: any) => {
    var data = Event.selectedRows;
    if (data.length == 1) this.setState({ enableEditButton: true });
    else this.setState({ enableEditButton: false });
    this.setState({ selectedRowData: data, Deactivate: data.length != 0 });
  };

  onSearch(e: any) {
    var { searchData } = this.state;
    var newValue = e?.target.value;
    newValue = newValue == undefined ? '' : newValue;
    var searchResult = searchData.filter((element: any) => {
      return element?.name
        ?.toLowerCase()
        .includes(newValue.trim().split(/ +/).join(' ').toLowerCase());
    });
    this.setState({ dataSource: searchResult, requestData: searchResult, searchKey: newValue });
  }

  render() {
    const { t } = this.props;
    var { requestData } = this.state;
    const columns: any[] = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{t('ROLE_USER_TABLE_SNO')}</div>,
        selector: (row: any) => row.index,
        isVisible: true,
        width: '100px',
      },
      {
        key: 'fileName',
        name: <div className={columnHeader}>{t('COL_FILE_NAME')}</div>,
        selector: (row: any) => row.name,
        isVisible: true,
      },
      {
        key: 'templates',
        name: <div className={columnHeader}>{t('COL_TEMPLATES')}</div>,
        selector: (row: any) =>
          this.state.templateDropdownOption.find((e: any) => e.key == row.templateId)?.text,
        isVisible: true,
      },
      {
        key: 'dateCreated',
        name: <div className={columnHeader}>{t('COL_DATE_CREATED')}</div>,
        selector: (row: any) => (
          <p>
            {row.dateCreated != null &&
              row.dateCreated != '' &&
              new Date(row.dateCreated).toLocaleDateString('en-GB')}
          </p>
        ),
        isVisible: true,
      },
      {
        key: 'action',
        selector: (row: any) => {
          var value = [row.id, row.name];
          return (
            <>
              {requestData.length != 0 && (
                <div>
                  <Link to={{ pathname: `/insights/datarequest/viewdata/${value}` }}>
                    <span>{t('VIEW_DATA')}</span>
                  </Link>
                </div>
              )}
            </>
          );
        },
        isVisible: true,
      },
    ];

    return (
      <div className="layout width-100">
        <Stack>
          {/* <Text block className="text">{`${t('PIM')} / ${t('OUTPUT')} / ${t('DATA_REQUEST')}`}</Text> */}
          <Text variant="xxLarge" styles={PIMHearderText} className="color-blue text">{`${t(
            'DATA_REQUEST',
          )}`}</Text>
        </Stack>
        <Grid lg={12} item container spacing={2} direction={'row'}>
          <Grid item lg={2} xs={6}>
            {this.state.marketId == 1 ? (
              <DefaultButton
                className="button"
                text={`${t('HOMECENTER')}`}
                styles={PIMButtons}
                onClick={() => this.templateOpt(1)}
              />
            ) : (
              <DefaultButton
                className="button"
                text={`${t('HOMECENTER')}`}
                styles={PIMHeaderButtons}
                onClick={() => this.templateOpt(1)}
              />
            )}
          </Grid>
          <Grid item lg={2} xs={6}>
            {this.state.marketId != 1 ? (
              <Dropdown
                className="dropdown"
                placeholder={this.props.t('MARKETPLACES')}
                options={this.state.marketDropdownOption}
                defaultSelectedKey={null}
                selectedKey={this.state.marketId == 1 ? null : this.state.marketId}
                onChange={(e: any, selectedOption: any) => {
                  this.templateOpt(selectedOption.key, 652);
                }}
                styles={AttributeheadDropdown}
                onRenderOption={renderOption}
              />
            ) : (
              <Dropdown
                className="dropdown"
                placeholder={this.props.t('MARKETPLACES')}
                options={this.state.marketDropdownOption}
                defaultSelectedKey={null}
                selectedKey={this.state.marketId == 1 ? null : this.state.marketId}
                onChange={(e: any, selectedOption: any) => {
                  this.templateOpt(selectedOption.key, 652);
                }}
                onRenderOption={renderOption}
              />
            )}
          </Grid>
          <Grid item lg={7.8} xs={0}></Grid>
        </Grid>
        <div style={boxStyles}>
          <Grid lg={12} item container spacing={2} direction={'row'}>
            <Grid item lg={2.5} xs={12}>
              <SearchBox
                className="searchBox"
                name={`${t('SEARCH_FILE_NAME')}`}
                styles={PIMsearchBoxStyles}
                placeholder={t('SEARCH_PLACEHOLDER')}
                onChange={(e: any) => this.onSearch(e)}
                value={this.state.searchKey}
                onClear={() => this.setState({ searchKey: '' })}
              />
            </Grid>
            <Grid item lg={1} xs={6}>
              <Text className={activeText}>{t('ACTIVE')}</Text>
            </Grid>
            <Grid item lg={1.3} xs={6}>
              <Dropdown
                className="dropdown"
                defaultSelectedKey={0}
                onChanged={(selectedOption: IDropdownOption) => {
                  this.setState({ isActive: selectedOption.key == 0 ? true : false });
                  this.refresh(!this.state.isActive);
                }}
                options={this.options}
                styles={PIMdropdownStyles}
              />
            </Grid>
            <Grid item lg={1.5} xs={6}>
              <DefaultButton
                className="button"
                text={
                  this.state.isActive
                    ? `${this.props.t('BTN_DEACTIVATE')}`
                    : `${this.props.t('BTN_ACTIVATE')}`
                }
                styles={PIMButtons}
                disabled={!this.state.Deactivate}
                onClick={() => this.setState({ confirmModel: true })}
              />
            </Grid>
            <Grid item lg={1.5} xs={6}>
              <DefaultButton
                className="button"
                iconProps={{ iconName: 'CircleAddition' }}
                text={`${this.props.t('ROLE_ADD_BTN')}`}
                styles={PIMButtons}
                disabled={!(this.state.isActive && this.state.selectedRowData.length == 0)}
                onClick={() => this.setState({ AddOrUpdate: !this.state.AddOrUpdate })}
              />
            </Grid>
            <Grid item lg={1.5} xs={2}>
              {this.state.marketId != 1 && (
                <Dropdown
                  className="dropdown"
                  options={this.state.departmentOption}
                  defaultSelectedKey={652}
                  selectedKey={this.state.productID}
                  styles={PIMdropdownStyles}
                  onChange={(e: any, selectedOption: any) =>
                    this.templateOpt(this.state.marketId, selectedOption.key)
                  }
                />
              )}
            </Grid>
            <Grid item lg={2} xs={0} />
            <Grid item lg={0.5} xs={2}>
              <Icon
                iconName="Refresh"
                title={t('BTN_REFRESH')}
                className="iconStyle"
                onClick={async () => {
                  this.refresh(this.state.isActive);
                }}
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
              {!this.state.isActive && (
                <Text variant="xLarge" className="apptext2">{`${t('CONFIRM_ACTIVATE')}`}</Text>
              )}
              {this.state.isActive && (
                <Text variant="xLarge" className="apptext2">{`${t('CONFIRM_DEACTIVATE')}`}</Text>
              )}
            </div>
            <div className={PIMcontentStyles.footer}>
              <Grid lg={12} item container spacing={2}>
                <Grid item lg={4} xs={12}>
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonConfirm}
                    text={`${t('BTN_CONFIRM')}`}
                    onClick={this.activeState.bind(this)}
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
          <Modal
            isOpen={this.state.AddOrUpdate}
            containerClassName={PIMcontentStyles.attributeContainer}
            onDismiss={() =>
              this.setState({
                AddOrUpdate: false,
              })
            }
          >
            <div className={PIMcontentStyles.header}>
              <div className="apptext1">{`${t('NEW_DATA_REQUEST')}`}</div>
              {/* <IconButton
                styles={CancelButtonStyles}
                iconProps={cancelIcon}
                ariaLabel="Close popup modal"
                onClick={() => {
                  this.setState({
                    AddOrUpdate: !this.state.AddOrUpdate,
                    noSelection: !this.state.noSelection,
                    enableEditButton: false,
                    enableActiveButton: false,
                    selectedRowData: [],
                  });
                }}
              /> */}
            </div>
            <div className={PIMcontentStyles.body}>
              <AddOrUpdateDataRequest
                fields={this.state.selectedRowData}
                templateDropdown={this.state.templateDropdownOption}
                reload={() => this.refresh()}
                ClosePopup={() => {
                  this.setState({
                    AddOrUpdate: !this.state.AddOrUpdate,
                    noSelection: !this.state.noSelection,
                    enableEditButton: false,
                    enableActiveButton: false,
                    selectedRowData: [],
                  });
                }}
              />
            </div>
          </Modal>

          <div className="dataTableFixedFooter">
            <DataTable
              columns={columns}
              data={requestData}
              pagination={true}
              selectableRows={this.state.selectedRowData}
              onSelectedRowsChange={this.rowSelection}
              clearSelectedRows={this.state.noSelection}
              selectableRowsHighlight
              highlightOnHover
              responsive
              fixedHeader
              fixedHeaderScrollHeight="386px"
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
        </div>
      </div>
    );
  }
}

const ComponentTranslated = withTranslation()(DataRequest);
function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
