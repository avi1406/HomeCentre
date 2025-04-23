import React, { Component, Suspense } from 'react';
import { Grid } from '@mui/material';
import { withTranslation } from 'react-i18next';
import { Text } from '@fluentui/react/lib/Text';
import ApiManager from '@/services/ApiManager';
import {
  DatePicker,
  DefaultButton,
  Dropdown,
  IChoiceGroupOption,
  IContextualMenuProps,
  IDropdownOption,
  Icon,
  Modal,
  PrimaryButton,
  SearchBox,
  Stack,
  TextField,
} from '@fluentui/react';
import DataTable from 'react-data-table-component';
import {
  PIMButtons,
  columnHeader,
  PIMHeaderButtons,
  productDropdown,
  ImgIcons1,
  ImgIcons,
  PIMsearchBoxStyles,
  Alartmessage,
  PIMdropdownStyles2,
  PIMHearderText,
} from '@/pages/PimStyles';
import { Link } from 'react-router-dom';
import AuthManagerService from '@/services/AuthManagerService';
import jwt_decode from 'jwt-decode';
import NotificationManager from '@/services/NotificationManager';
import CommonDate, {
  CommonDateRef,
  downloadFileItemcode,
  downloadFileMarket,
  renderOption,
} from '@/pages/commonFunctions';

import { baseUrl } from '@/services/Constants';
import axios from 'axios';
import Util from '@/services/Util';
import { MarketplaceTemplate } from '@/services/ApiClient';

class CatalogMapping extends Component<any, any> {
  apiClient = new ApiManager().CreateApiClient();
  authManager = new AuthManagerService();
  notify = new NotificationManager();
  token = sessionStorage.getItem('token');
  decode: any = jwt_decode(this.token || '');
  selectedMarketPlace: any;
  roleId: any = this.decode.RoleId.split(',').map(Number);
  isAuthenticated = this.authManager.isAuthenticated();
  user = this.isAuthenticated ? this.authManager.getUserData() : null;
  departmentId: any[] = this.user?.departmentId || [];
  util = new Util();
  statusOptions: any = [
    { key: 0, text: this.props.t('ALL') },
    { key: 1, text: this.props.t('READY') },
  ];
  selectedDivision: any = 0;
  constructor(props: any) {
    super(props);
    this.state = {
      templateDropdownOption: [],
      templateData: [],
      templateDatas: [],
      isRecord: false,
      isActive: true,
      marketPlaceTemplateId: 0,
      userDivision: null,
      productDeptId: null,
      departmentOption: [],
      departmentOption1: [],
      marketPlaceId: 1,
      marketplaceName: this.props.t('SELECT_MARKETPLACE'),
      fromDate: null,
      toDate: null,
      statusOptions: 0,
      deptOption: [],
      searchDivision1: null,
      searchDivision2: null,
      rowCounter: 0,
    };
  }
  commonDateRef = React.createRef<CommonDateRef>();
  async componentDidMount(): Promise<void> {
    this.setState({ isRecord: true });
    this.FilterFun();
    var option: any = [];
    var deptOpt: any = [];
    const tempdrop = await this.apiClient.searchMarketplaces('', 0, 0, true);
    // const depyResponse = (await this.apiClient.getHierarchicalMasters()).result;
    tempdrop.result?.records?.forEach((element: any) => {
      option.push({ key: element.id, text: element.name });
    });
    var deptOptCG: {
      key: number | undefined;
      text: string | undefined;
      parentCode: number | undefined;
    }[] = [];
    // depyResponse?.forEach((e: any) => {
    //   deptOptCG.push({ key: e.dept, text: e.dept_Name, parentCode: e.division });
    // });
    // depyResponse?.forEach((e: any) => {
    //   deptOpt.push({ key: e.dept, text: e.dept_Name });
    // });
    const options = option.filter((item: any) => item.key !== 1);
    this.setState({
      templateDropdownOption: options,
      // departmentOption: deptOpt,
      // departmentOption1: deptOptCG,
    });
    this.selectedMarketPlace = option?.filter((element: any) => element.key == 1)[0];
    var departmentList = (await this.apiClient.getDivision())?.result;
    var deptOpt1: { key: number | undefined; text: string | undefined }[] = [];
    departmentList?.forEach((element) => {
      deptOpt1.push({ key: element.id, text: element.name });
    });
    this.setState({
      deptOption: deptOpt1,
    });
    this.refresh(this.state.isActive, null);
  }

  async refresh(isActive: boolean, productId?: any) {
    this.setState({ isRecord: true });
    const Ids: any = this.util.getUrlParameterId();
    const id: any = Ids.split(',');
    const viewDataId: any = parseInt(id[0]);
    const divisionId = this.selectedDivision;
    var marketPlaceId =
      this.selectedMarketPlace?.key == undefined ? 1 : this.selectedMarketPlace.key;
    var productDepartmentId = productId == null ? 0 : productId;
    let response;
    response = await this.apiClient.searchMarketplaceTemplatesCam(
      '',
      0,
      0,
      marketPlaceId,
      divisionId,
      productDepartmentId,
      isActive,
    );

    const records = response.result?.records || [];
    const nameToSet = records.length > 0 ? records[0].name : '';
    const firstRecord = records.length > 0 ? records[0] : null;
    this.setState({
      templateData: response.result?.records?.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      })),
      marketPlaceId: marketPlaceId,
      isRecord: false,
      productDeptId: productDepartmentId,
      fromDate: null,
      toDate: null,
      statusOptions: 0,
      id: firstRecord?.id,
      marketPlaceTemplateId: viewDataId,
    });
    this.setState({
      searchAdmindata: response.result?.records?.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      })),
    });
    var marketplaceTemplateId = this.state.id == undefined ? 0 : this.state.id;
    if (
      !this.roleId.includes(1) &&
      marketplaceTemplateId != 0 &&
      marketplaceTemplateId != undefined
    ) {
      var marketplaceTemplateId = this.state.id == undefined ? 0 : this.state.id;
      var status: any = 0;
      var fromDate = this.state.fromDate ? this.state.fromDate.toISOString() : '';
      var toDate = this.state.toDate ? this.state.toDate.toISOString() : '';
      const respons = await this.apiClient.getItemCode(
        fromDate,
        toDate,
        status,
        marketplaceTemplateId,
      );

      this.setState({
        templateDatas: respons.result?.map((item: any, index: any) => ({
          index: index + 1,
          ...item,
        })),
      });
      this.setState({
        searchData: respons.result?.map((item: any, index: any) => ({ index: index + 1, ...item })),
      });
    }
    if (this.commonDateRef.current) {
      this.commonDateRef.current.resetDates();
    }
  }
  async FilterFun() {
    const MasterData = (await this.apiClient.getProductdeptMasters()).result;
    const departmentSet = new Set();
    const departmentMap = new Map();

    MasterData?.forEach((element) => {
      if (this.departmentId.includes(element.division)) {
        const deptdata = {
          dept_Name: element.name,
          dept: element.code,
          division: element.division,
          id: element.id,
        };
        const deptdropdown = {
          dept_Name: element.name,
          id: element.id,
          dept: element.code,
        };
        const key = `${deptdata.dept_Name}_${deptdata.dept}_${deptdata.division}`;
        let isUnique = true;
        departmentMap.forEach((existingData: any) => {
          if (
            existingData.dept_Name === deptdata.dept_Name &&
            existingData.dept === deptdata.dept &&
            existingData.division === deptdata.division &&
            existingData.id === deptdata.id
          ) {
            isUnique = false;
          }
        });
        if (isUnique) {
          departmentMap.set(key, deptdata);
          departmentSet.add(deptdropdown);
        }
      }
    });
    // const departmentList = Array.from(departmentSet).map((deptName) => ({
    //   text: deptName,
    // }));
    const departmentList = Array.from(departmentSet)
      .map((deptName: any) => ({
        text: deptName.dept_Name,
        ket: deptName.dept,
      }))
      .sort((a: any, b: any) => {
        return a.text.localeCompare(b.text);
      });
    this.setState({
      departmentOption: departmentList,
      departmentOption1: Array.from(departmentMap.values()),
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
  handleMenuItemClick = (ev: any, item: any) => {
    this.setState({ marketplaceName: item.text, marketPlaceId: item.key });
    this.selectedDivision = 0;
    this.selectedMarketPlace = item;
    this.refresh(true, null);
  };

  async searchProducts(statusOptions?: any) {
    var data: any = [];
    var fromDate = this.state.fromDate
      ? encodeURIComponent(this.state.fromDate.toLocaleDateString('en-CA'))
      : undefined;
    var toDate = this.state.toDate
      ? encodeURIComponent(this.state.toDate.toLocaleDateString('en-CA'))
      : undefined;
    var marketplaceTemplateId = this.state.id == undefined ? 0 : this.state.id;
    const status: any = statusOptions == 0 ? 0 : 1;
    const respons = (
      await this.apiClient.getItemCode(fromDate, toDate, status, marketplaceTemplateId)
    ).result;
    respons?.forEach((e: any) => {
      if (data.filter((element: any) => element.itemCode == e.itemCode).length == 0) {
        data.push(e);
      }
    });

    this.setState({
      templateDatas: data?.map((item: any, index: any) => ({ index: index + 1, ...item })),
    });
    this.setState({ searchStatus: status });
  }

  handleMinDateChange = (date: any) => {
    this.setState({ fromDate: date });
  };

  handleToDateChange = (date: any) => {
    this.setState({ toDate: date });
  };
  ItemcodeTemplate = async () => {
    try {
      const fromDate = this.state.fromDate
        ? encodeURIComponent(this.state.fromDate.toLocaleDateString('en-CA'))
        : undefined;
      const toDate = this.state.toDate
        ? encodeURIComponent(this.state.toDate.toLocaleDateString('en-CA'))
        : undefined;
      const marketplaceTemplateId = this.state.templateData[0].id;
      const token = this.token;
      let url = `${baseUrl}/api/Template/ItemCodeDownload`;
      url += `?${Object.entries({ token })
        .map(([key, value]) => `${key}=${value}`)
        .join('&')}`;
      if (fromDate) {
        url += `&fromDate=${fromDate}`;
      }
      if (toDate) {
        url += `&toDate=${toDate}`;
      }
      if (marketplaceTemplateId) {
        url += `&marketplaceTemplateId=${marketplaceTemplateId}`;
      }
      // axios(url, {
      //   method: 'GET',
      //   headers: {
      //     'Content-Type': 'application/json;',
      //   },
      // }).then((response: any) => {
      //   if (response.data.hasError === true) {
      //     return this.notify.showErrorNotify(response.data.message);
      //   } else {
      downloadFileItemcode(url);
      // }
      // });
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
  };

  ItemcodeTemplate1 = async () => {
    try {
      var marketPlaceTemplateId = this.state.id;
      axios(
        baseUrl +
          `/api/Template/MarketPlaceDownload?marketplacetemplateid=${marketPlaceTemplateId}`,
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
          const url = `${baseUrl}/api/Template/MarketPlaceDownload?marketplacetemplateid=${marketPlaceTemplateId}`;
          downloadFileMarket(url);
        }
      });
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
  };
  handleDropdownChange = (option: any) => {
    if (option) {
      this.setState({ searchDivision: option.text, productDeptId: option.key });
    }
  };
  areConditionsMet() {
    if (
      this.state.productDeptId !== 0 &&
      this.state.searchDivision !== null &&
      this.selectedDivision !== 0
    ) {
      return true;
    } else {
      return false;
    }
  }
  showAlert = () => {
    this.setState({ showAlert: true });
  };

  hideAlert = () => {
    this.setState({ showAlert: false });
  };
  onSearch(e: any) {
    var newValue = e?.target.value;
    newValue = newValue == undefined ? '' : newValue;
    this.setState({ searchValue: newValue, searchKey: newValue });
    var result = this.state.searchData.filter((element: any) => {
      return element?.itemCode
        ?.toLowerCase()
        .includes(newValue.trim().split(/ +/).join(' ').toLowerCase());
    });
    this.setState({ templateDatas: result });
    this.setState({
      templateDatas: result?.map((item: any, index: any) => ({ index: index + 1, ...item })),
    });
  }

  onSearchAdmin(e: any) {
    var newValue = e?.target.value;
    newValue = newValue == undefined ? '' : newValue;
    this.setState({ searchValue: newValue, searchKey: newValue });
    var result = this.state.searchAdmindata.filter((element: any) => {
      return element?.itemCode
        ?.toLowerCase()
        .includes(newValue.trim().split(/ +/).join(' ').toLowerCase());
    });
    this.setState({ templateData: result });
    this.setState({
      templateData: result?.map((item: any, index: any) => ({ index: index + 1, ...item })),
    });
  }
  onChange = (ev: React.FormEvent<HTMLInputElement>, option: IChoiceGroupOption) => {
    this.setState({
      selectedDepartment: option.key,
      selectedDivisionId: option.key,
    });
    var filteredDivision: any = [];
    filteredDivision.push({ key: option.key, text: option.text });
    var deptOpt: any = [];
    const filteredDivOpt1 = this.state.departmentOption1?.filter((department: any) => {
      return filteredDivision.some((filter: any) => department.division === filter.key);
    });
    filteredDivOpt1?.forEach((element: any) => {
      deptOpt.push({ key: element.id, text: element.dept_Name });
    });
    const departmentList = Array.from(deptOpt)
      .sort((a: any, b: any) => {
        return a.text.localeCompare(b.text);
      })
      .map((deptName: any) => ({
        text: deptName.text,
        key: deptName.key,
      }));
    this.setState({ departmentOption: departmentList });
    this.selectedDivision = option.key;
    this.refresh(this.state.isActive, null);
  };
  render() {
    const { t } = this.props;
    const columns: any = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{`${t('S_NO')}`}</div>,
        // selector: (row: any) => row.index,
        cell: (row: any) => row.index,
        sortable: false,
        isVisible: true,
        maxWidth: '250px',
      },
      {
        key: 'templates',
        name: <div className={columnHeader}>{`${t('TEMPLATES')}`}</div>,
        selector: (row: any) => {
          var value = [row.id, row.divisionId, row.name, this.state.marketPlaceId];
          return (
            <div>
              <Link to={{ pathname: `/configuration/CatalogMapping/templateAttibute/${value}` }}>
                <span title={row.name}>{row.name}</span>
              </Link>
            </div>
          );
        },
        isVisible: true,
        minwidth: '500px',
      },
      {
        key: 'marketplace',
        name: <div className={columnHeader}>{`${t('TEMPLATES_MARKET')}`}</div>,
        selector: (row: any) => {
          var MarketPlace;
          if (row.marketplaceId != 1) {
            MarketPlace = this.state.templateDropdownOption?.find(
              (e: any) => e.key == row.marketplaceId,
            )?.text;
          } else {
            MarketPlace = `${t('HOMECENTER')}`;
          }
          return <span title={MarketPlace}>{MarketPlace}</span>;
        },
        isVisible: true,
        minwidth: '500px',
      },
      {
        key: 'division',
        name: <div className={columnHeader}>{`${t('TEMPLATES_DIVISION')}`}</div>,
        selector: (row: any) => {
          var Division = this.state.deptOption?.find((e: any) => e.key == row.divisionId)?.text;
          return <span title={Division}>{Division}</span>;
        },
        isVisible: true,
        minwidth: '500px',
      },
      {
        key: 'dept',
        name: <div className={columnHeader}>{`${t('TEMPLATES_DEPT')}`}</div>,
        selector: (row: any) => {
          var dept = this.state.departmentOption1?.find(
            (e: any) => e.id == row.productDepartmentId,
          )?.dept_Name;
          return <span title={dept}>{dept}</span>;
        },
        isVisible: true,
        minwidth: '500px',
      },
      {
        key: 'dateCreated',
        name: <div className={columnHeader}>{`${t('DATE_CREATED')}`}</div>,
        selector: (row: any) => {
          const formattedDate =
            row.dateCreated != null && row.dateCreated != ''
              ? new Date(row.dateCreated).toLocaleDateString('en-GB')
              : '';
          return <span title={formattedDate}>{formattedDate}</span>;
        },
        isVisible: true,
      },
      {
        key: 'status',
        name: <div className={columnHeader}>{`${t('COL_STATUS')}`}</div>,
        selector: (row: any) => {
          if (
            this.state.templateData != null &&
            this.state.templateData != undefined &&
            this.state.templateData.length > 0
          ) {
            return (
              <span title={row.isActive ? 'true' : 'false'}>{row.isActive ? 'true' : 'false'}</span>
            );
          }
        },
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

    const menuProps: IContextualMenuProps = {
      shouldFocusOnMount: true,
      items: this.state.templateDropdownOption.map((item: any) => ({
        ...item,
        onClick: this.handleMenuItemClick,
      })),
    };
    const onformsubmit = async () => {
      const addTemplate = new MarketplaceTemplate();
      addTemplate.name = this.state.selectedOption.text;
      addTemplate.marketplaceId = this.state.marketPlaceId;
      addTemplate.productDepartmentId =
        this.state.productDepartmentId == 1 ? 652 : this.state.productDeptId;
      addTemplate.isActive = true;
      addTemplate.divisionId = this.selectedDivision;
      try {
        this.setState({ Template: false, fields: { templateName: '' } });
        this.apiClient.addOrUpdateMarketplaceTemplate(addTemplate).then((r: any) => {
          let message = r.hasError == false ? `${t('MSG_ADD')}` : `${t('ERROR_UNKNOWN')}`;
          this.refresh(true, this.state.productDeptId);
          this.setState({ divisionId: 0 });
          if (r.hasError) {
            this.notify.showErrorNotify(r.message);
          } else {
            this.notify.showSuccessNotify(message);
          }
        });
      } catch (e: any) {
        this.notify.showErrorNotify(e);
      }
    };
    const selection = () => {
      if (this.state.templateData == undefined) {
        return false;
      } else {
        if (this.state.templateData.length !== 0 && this.state.productDeptId !== 0) {
          return true;
        } else {
          return false;
        }
      }
    };
    const data = () => {
      const a = [25, 50, 100, 150, 200];
      return a;
    };
    return (
      <>
        <div className="layout width-100">
          <Stack>
            <Text variant="xxLarge" styles={PIMHearderText} className="color-blue text">{`${t(
              'MENU_CATALOG_MAPPING',
            )}`}</Text>
          </Stack>
          <Grid lg={12} item container spacing={2} direction={'row'}>
            <Grid item lg={2} xs={5}>
              <DefaultButton
                className="button"
                text={t('HOMECENTER')}
                styles={this.state.marketPlaceId == 1 ? PIMButtons : PIMHeaderButtons}
                onClick={() => {
                  this.setState({
                    marketPlaceId: 1,
                    marketplaceName: '-Select marketplace-',
                    searchDivision1: null,
                  });
                  this.componentDidMount();
                  this.selectedDivision = 0;
                }}
              />
            </Grid>
            <Grid item lg={2} xs={5}>
              <DefaultButton
                className="button"
                text={this.state.marketplaceName}
                styles={this.state.marketPlaceId != 1 ? PIMButtons : PIMHeaderButtons}
                menuProps={menuProps}
              />
            </Grid>
            <Grid item lg={7.4} xs={0} />
            <Grid item lg={0.5} xs={1.2}>
              {/* <Icon
                iconName="Download"
                title={t('BTN_DOWNLOAD')}
                className="iconStyle"
                onClick={() => {
                  if (selection()) {
                    this.ItemcodeTemplate();
                  }
                }}
              /> */}
            </Grid>
          </Grid>
          {/* <br /> */}
          <div className="Catalog"></div>
          <Grid lg={12} item container spacing={2} direction={'row'}>
            <Grid item lg={1.98} xs={6}>
              <Dropdown
                className="dropdown"
                options={this.state.deptOption}
                styles={PIMdropdownStyles2}
                selectedKey={this.selectedDivision}
                onChange={(event: any, selectedOption: any) => this.onChange(event, selectedOption)}
                placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                style={{ marginTop: -4 }}
              />
            </Grid>
            <Grid item lg={2} xs={6}>
              <Dropdown
                className="dropdown"
                options={this.state.departmentOption}
                styles={productDropdown}
                placeholder={`${t('DROPDOWN_SELECT_CATEGORY')}`}
                onRenderOption={renderOption}
                selectedKey={this.state.productDeptId}
                onChanged={(selectedOption: IDropdownOption) => {
                  this.handleDropdownChange(selectedOption);
                  this.setState({ productDeptId: selectedOption, selectedOption });
                  this.refresh(true, selectedOption.key);
                }}
              />
            </Grid>
            <Grid item lg={1.2} xs={6}>
              <span title={t('ROLE_ADD_BTN')}>
                <Icon
                  iconName="CircleAddition"
                  className="buttonIcon5"
                  onClick={async () => {
                    if (this.areConditionsMet()) {
                      onformsubmit();
                    } else {
                      this.showAlert();
                    }
                  }}
                  style={{
                    cursor: this.areConditionsMet() ? 'not-allowed' : 'pointer',
                    color: this.areConditionsMet() ? 'gray' : 'inherit',
                  }}
                />
              </span>
            </Grid>

            <Grid item lg={6.2} xs={0} />
            <Grid item lg={0.5} xs={2}>
              <Icon
                iconName="Refresh"
                title={t('BTN_REFRESH')}
                className="iconStyle"
                onClick={() => {
                  this.setState({
                    marketplaceName: '-Select marketplace-',
                    searchDivision1: null,
                    productDeptId: null,
                  });
                  this.selectedDivision = 0;
                  this.componentDidMount();
                  this.refresh(true, null);
                }}
              />
            </Grid>
          </Grid>
          <Modal
            isOpen={this.state.showAlert}
            isBlocking={false}
            containerClassName="custom-modal"
            onDismiss={() =>
              this.setState({
                showAlert: false,
              })
            }
          >
            <div className="modal-header">
              <span>Error</span>
            </div>
            <div className="modal-content">
              <p>Please select Division and Category before creating template.</p>
            </div>
            <div className="modal-footer">
              <DefaultButton className="modal-footer1" onClick={this.hideAlert} text="Close" />
            </div>
          </Modal>
          <div className="dataTableFixedFooter">
            <DataTable
              className="datatable1"
              columns={columns}
              data={this.state.templateData}
              onColumnOrderChange={handleColumnReorder}
              pagination={true}
              selectableRowsHighlight
              highlightOnHover
              responsive
              fixedHeader
              striped
              fixedHeaderScrollHeight="58.5vh"
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
      </>
    );
  }
}
const ComponentTranslated = withTranslation()(CatalogMapping);
function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}
export default App;
