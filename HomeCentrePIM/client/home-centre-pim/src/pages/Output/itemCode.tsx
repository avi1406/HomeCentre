import {
  ComboBox,
  DatePicker,
  DefaultButton,
  Dropdown,
  IDropdownOption,
  Icon,
  SearchBox,
  Stack,
  mergeStyles,
} from '@fluentui/react';
import React, { Suspense } from 'react';
import { withTranslation } from 'react-i18next';
import { Text } from '@fluentui/react/lib/Text';
import { Grid } from '@mui/material';
import {
  columnHeader,
  ImgIcons,
  ImgIcons1,
  PIMButtons,
  PIMdropdownStyles2,
  PIMHearderText,
  PIMsearchBoxStyles,
  productDropdown,
} from '../PimStyles';
import DataTable from 'react-data-table-component';
import NotificationManager from '@/services/NotificationManager';
import ApiManager from '@/services/ApiManager';
import { baseUrl } from '@/services/Constants';
import {
  downloadFileItemCodeAll,
  downloadFileItemCodeReady,
  renderOption,
} from '../commonFunctions';
import CommonDate, { CommonDateRef } from '../commonFunctions';
import AuthManagerService from '@/services/AuthManagerService';
import { ItemCodeProducts } from '@/services/ApiClient';

class ItemCode extends React.Component<any, any> {
  notify = new NotificationManager();
  apiClient = new ApiManager().CreateApiClient();
  token: any = sessionStorage.getItem('token');
  authManager = new AuthManagerService();
  isAuthenticated = this.authManager.isAuthenticated();
  user = this.isAuthenticated ? this.authManager.getUserData() : null;
  deptId: any[] = this.user?.departmentId || [];
  selectedMarketPlace: any;
  statusOptions: any = [
    { key: 0, text: this.props.t('ALL') },
    { key: 2, text: this.props.t('NOT_READY') },
    { key: 1, text: this.props.t('READY') },
  ];
  data1: any = [];
  data2: any = [];

  constructor(props: any) {
    super(props);
    this.state = {
      uidData: [],
      searchData: [],
      isRecord: false,
      searchKey: '',
      departmentOptionItemcode: [],
      departmentOptionDP: [],
      departmentOption: [],
      divisionOption: [],
      productDeptId: 0,
      getdept: [],
      previousStatusOptions: 0,
      isDropdownOpen: false,
      statusOptions: 0,
      fromDate: null,
      DivdeptOption: [],
      productDeptId1: [],
      toDate: null,
      downloadenable: false,
      Isdata: false,
      downloadFromDate: null,
      downloadToDate: null,
      downloadProductDept: 0,
      downloaStatusOption: 0,
    };
  }

  commonDateRef = React.createRef<CommonDateRef>();

  // async componentDidMount() {
  //   this.setState({ isRecord: true });
  //   var division: any = [];
  //   var divisionId: any = this.authManager.getUserData()?.departmentId;
  //   const divisions = await this.apiClient.getDivision();
  //   divisions?.result?.forEach((element: any) => {
  //     division.push({ key: element.id, text: element.name });
  //   });
  //   var filteredDivision = division?.filter((o1: any) =>
  //     divisionId.some((o2: any) => o1.key === o2),
  //   );
  //   var option: any = [];
  //   var deptOpt: any = [];
  //   const tempdrop = await this.apiClient.searchMarketplaces('', 0, 0, true);
  //   const depyResponse = (await this.apiClient.getDepartments()).result;
  //   const MasterData = (await this.apiClient.getHierarchicalMasters()).result;
  //   const departmentSet = new Set();
  //   MasterData?.forEach((element: any) => {
  //     if (element && this.deptId.includes(element.division)) {
  //       const dept = {
  //         dept: element.dept,
  //         Name: element.dept_Name,
  //         division: element.division,
  //       };
  //       const isDuplicate = Array.from(departmentSet).some((item: any) => item.dept === dept.dept);
  //       if (!isDuplicate) {
  //         departmentSet.add(dept);
  //       }
  //     }
  //   });

  //   const department = Array.from(departmentSet).map((deptName: any) => ({
  //     key: deptName.dept,
  //     text: deptName.Name,
  //     division: deptName.division,
  //   }));
  //   tempdrop.result?.records?.forEach((element: any) => {
  //     option.push({ key: element.id, text: element.name });
  //   });
  //   var deptOptCG: {
  //     key: number | undefined;
  //     text: string | undefined;
  //     parentCode: number | undefined;
  //   }[] = [];
  //   var CatogaryDropdown: {
  //     key: number | undefined;
  //     text: string | undefined;
  //     parentCode: number | undefined;
  //   }[] = [];
  //   department?.forEach((e: any) => {
  //     const parentCodeAsNumber = parseInt(e.division, 10);
  //     if (this.deptId.includes(parentCodeAsNumber)) {
  //       CatogaryDropdown.push({ key: e.key, text: e.text, parentCode: e.division });
  //     }
  //     deptOptCG.push({ key: e.dept, text: e.name, parentCode: e.division });
  //   });

  //   const filteredDivOpt1 = deptOptCG?.filter((department: any) => {
  //     return filteredDivision.some(
  //       (filter: any) => department.parentCode === filter.key.toString(),
  //     );
  //   });

  //   filteredDivOpt1?.forEach((element) => {
  //     deptOpt.push({ key: element.key, text: element.text, parentCode: element.parentCode });
  //   });
  //   const options = option.filter((item: any) => item.key !== 1);
  //   this.setState({
  //     getdept: department,
  //     templateDropdownOption: options,
  //     departmentOptionItemcode: CatogaryDropdown,
  //     departmentOptionDP: CatogaryDropdown,
  //     departmentOption1: deptOptCG,
  //     productDeptId1: [],
  //     DivdeptOption: [],
  //   });
  //   this.selectedMarketPlace = option?.filter((element: any) => element.key == 1)[0];
  //   var departmentList = (await this.apiClient.getDivision())?.result;

  //   var deptOpt112: { key: number | undefined; text: string | undefined }[] = [];
  //   departmentList?.forEach((element) => {
  //     deptOpt112.push({ key: element.id, text: element.name });
  //   });
  //   var filteredDivOpt = deptOpt112?.filter((department) =>
  //     filteredDivision.some((filter: any) => department.key === filter.key),
  //   );
  //   var deptOpt1: { key: number | undefined; text: string | undefined }[] = [];
  //   filteredDivOpt?.forEach((element) => {
  //     deptOpt1.push({ key: element.key, text: element.text });
  //   });
  //   this.setState({
  //     deptOption: deptOpt1,
  //   });
  //   this.refresh();
  // }
  async componentDidMount() {
    this.setState({ isRecord: true });
  
    try {
      const divisionId = this.authManager.getUserData()?.departmentId;
  
      const [divisionsResponse, tempdropResponse, depyResponse, masterDataResponse] = await Promise.all([
        this.apiClient.getDivision(),
        this.apiClient.searchMarketplaces('', 0, 0, true),
        this.apiClient.getDepartments(),
        this.apiClient.getHierarchicalMasters(),
      ]);
  
      const divisions = divisionsResponse?.result || [];
      const tempdrop = tempdropResponse?.result?.records || [];
      const MasterData = masterDataResponse?.result || [];
  
      const filteredDivision = divisions?.filter((division: any) => divisionId?.includes(division.id));
  
      const departmentSet = new Set();
      MasterData?.forEach((element: any) => {
        if (element && this.deptId.includes(element.division)) {
          const dept = { dept: element.dept, Name: element.dept_Name, division: element.division };
          if (!Array.from(departmentSet).some((item: any) => item.dept === dept.dept)) {
            departmentSet.add(dept);
          }
        }
      });
  
      const department = Array.from(departmentSet).map((dept: any) => ({
        key: dept.dept,
        text: dept.Name,
        division: dept.division,
      }));
  
      const option = tempdrop.map((element: any) => ({ key: element.id, text: element.name }));
  
      const deptOptCG = department.map((e: any) => ({
        key: e.key,
        text: e.text,
        parentCode: parseInt(e.division, 10),
      }));
  
      const CatogaryDropdown = deptOptCG.filter((e: any) => this.deptId.includes(e.parentCode));
  
      const filteredDivOpt1 = deptOptCG.filter((department: any) =>
        filteredDivision.some((filter: any) => department.parentCode === filter.id),
      );
  
      const deptOpt = filteredDivOpt1.map((element: any) => ({
        key: element.key,
        text: element.text,
        parentCode: element.parentCode,
      }));
  
      const filteredOptions = option.filter((item: any) => item.key !== 1);
      this.selectedMarketPlace = option.find((element: any) => element.key == 1);
  
      const departmentList = (await this.apiClient.getDivision())?.result || [];
      const deptOpt112 = departmentList.map((element: any) => ({
        key: element.id,
        text: element.name,
      }));
  
      const filteredDivOpt = deptOpt112.filter((department: any) =>
        filteredDivision.some((filter: any) => department.key === filter.id),
      );
  
      const deptOpt1 = filteredDivOpt.map((element: any) => ({
        key: element.key,
        text: element.text,
      }));
  
      this.setState({
        getdept: department,
        templateDropdownOption: filteredOptions,
        departmentOptionItemcode: CatogaryDropdown,
        departmentOptionDP: CatogaryDropdown,
        departmentOption1: deptOptCG,
        productDeptId1: [],
        DivdeptOption: [],
        deptOption: deptOpt1,
      });
  
      this.refresh();
    } catch (e: any) {
      this.notify.showErrorNotify(e.message);
      this.setState({ isRecord: false });
    }
  }  

  async refresh() {
    this.setState({ isRecord: true, searchKey: '' });
    var deptOpt: any = [];
    var divOpt: any = [];
    const depResponse = (await this.apiClient.getDepartments()).result;
    const div = (await this.apiClient.getDivision()).result;
    const filteredResponse = depResponse?.filter((item: any) =>
      this.deptId.includes(Number(item.parentCode)),
    );
    filteredResponse?.forEach((e: any) => {
      deptOpt.push({ key: e.id, text: e.name });
    });
    if (depResponse != null) {
      depResponse?.forEach((row: any) => {
        this.data1.push(row);
      });
    }
    div?.forEach((e: any) => {
      divOpt.push({ key: e.id, text: e.name });
    });
    if (div != null) {
      div?.forEach((row: any) => {
        this.data2.push(row);
      });
    }
    this.setState(
      {
        departmentOption: deptOpt,
        divisionOption: divOpt,
      },
      () => {
        this.searchProducts(
          this.state.productDeptId1,
          this.state.statusOptions,
          this.state.fromDate,
          this.state.toDate,
        );
      },
    );
  }

  async searchProducts(productDeptId?: any, statusOptions?: any, fromDate?: any, toDate?: any) {
    var data: any = [];
    const startDate: any = fromDate == null ? undefined : fromDate?.toLocaleDateString('en-CA');
    const endDate: any = toDate == null ? undefined : toDate?.toLocaleDateString('en-CA');
    const searchStatus: any = statusOptions == 0 ? 0 : 1;
    let resultString;
    if (this.state.DivdeptOption.length == 0) {
      let filteredData = this.deptId.filter((item) => item !== 0);
      resultString = filteredData.join(',');
    } else {
      resultString = this.state.DivdeptOption.map((divDeptText: string) => {
        const division = this.state.deptOption.find((e: any) => e.text === divDeptText);
        return division ? division.key : null;
      });
    }
    if (productDeptId != 0 && productDeptId != null) {
      const deptkey = productDeptId.map((DeptText: string) => {
        const depetname = this.state.departmentOptionItemcode.find((e: any) => e.text === DeptText);
        return depetname ? depetname.key : null;
      });
      productDeptId = deptkey.join(',');
    }
    const response = (
      await this.apiClient.listProductsItem(
        startDate,
        endDate,
        statusOptions,
        productDeptId,
        resultString,
      )
    ).result;
    const filteredResponse = response; //?.filter((item: any) =>
    //   this.deptId.includes(item.columnData['Division Code']),
    // ) as ItemCodeProducts[];

    if (filteredResponse && filteredResponse?.length > 0) {
      this.setState({ Isdata: true });
      if (this.dropdownOptions.length === 0) {
        const newItem: IDropdownOption = {
          key: 'exportScreen',
          text: this.props.t('Export Itemcode Current View'),
        };
        this.dropdownOptions.push(newItem);
      }
      const ready = filteredResponse?.filter((e: any) => e.status == 1);
      if (
        ready.length > 0 &&
        (this.state.DivdeptOption.length != 0 ||
          this.state.fromDate != null ||
          this.state.toDate != null)
      ) {
        if (this.dropdownOptions.length === 1) {
          const newItem: IDropdownOption = {
            key: 'exportReady',
            text: this.props.t('Export Itemcode Data'),
          };
          this.dropdownOptions.push(newItem);
        }
      } else {
        this.setState({ downloadenable: false });
      }
      if (
        this.state.DivdeptOption.length == 0 &&
        this.state.fromDate == null &&
        this.state.toDate == null
      ) {
        if (this.dropdownOptions.length > 1) {
          this.dropdownOptions = this.dropdownOptions.slice(0, 1);
        }
      }
    } else {
      this.setState({ downloadenable: false, Isdata: false });
    }
    this.setState({
      uidData:
        filteredResponse && filteredResponse?.length > 0
          ? filteredResponse.map((item: any, index: any) => ({
              index: index + 1,
              ...item,
            }))
          : [],
      searchData:
        filteredResponse && filteredResponse?.length > 0
          ? filteredResponse?.map((item: any, index: any) => ({ index: index + 1, ...item }))
          : [],
      isRecord: false,
      downloadFromDate: fromDate,
      downloadToDate: toDate,
      downloaStatusOption: searchStatus,
      downloadProductDept: productDeptId,
      searchStatus: statusOptions,
    });
  }
  DropdownFilter = async () => {
    const catogoryArray: { key: number; text: string }[] = [];
    const selectedKeys = this.state.deptOption
      .filter((option: any) => this.state.DivdeptOption.includes(option.text))
      .map((option: any) => option.key);
    const filteredOptions = this.state.departmentOptionItemcode.filter((option: any) => {
      const parentCodeAsNumber = parseInt(option.parentCode, 10);
      return selectedKeys.includes(parentCodeAsNumber);
    });
    filteredOptions.forEach((filteredOption: any) => {
      catogoryArray.push({ key: filteredOption.key, text: filteredOption.text });
    });
    const catogory = catogoryArray.sort((a, b) => a.text.localeCompare(b.text));
    this.setState({ departmentOptionDP: catogory });
  };
  private dropdownOptions: IDropdownOption[] = [];
  toggleDropdown = () => {
    this.setState((prevState: any) => ({ isDropdownOpen: !prevState.isDropdownOpen }));
    document.body.addEventListener('click', this.handleOutsideClick);
  };
  closeDropdown = () => {
    this.setState({ isDropdownOpen: false });
    document.body.removeEventListener('click', this.handleOutsideClick);
  };

  handleOutsideClick = (event: any) => {
    const dropdownElement = document.querySelector('.dropdown-container');
    if (dropdownElement && !dropdownElement.contains(event.target)) {
      this.closeDropdown();
    }
  };
  handleMouseEnter = () => {
    this.setState({ isMouseOverDropdown: true });
  };

  handleMouseLeave = () => {
    this.setState({ isMouseOverDropdown: false, hoveredOption2: false, hoveredOption1: false });
  };

  handleOptionMouseEnter = (option: any) => {
    this.setState({ hoveredOption: option });
  };

  handleOptionMouseLeave = () => {
    this.setState({ hoveredOption: null });
  };
  perpage = () => {
    const recordCont = [25, 50, 100, 150, 200];
    return recordCont;
  };
  Defaultperpage = () => {
    const recordCont = 25;
    return recordCont;
  };
  ClearDates() {
    if (this.commonDateRef.current) {
      this.commonDateRef.current.resetDates();
    }
  }
  handleMinDateChange = (date: any) => {
    this.setState({ fromDate: date });
  };

  handleToDateChange = (date: any) => {
    this.setState({ toDate: date });
  };

  ItemcodeTemplate = async (event: any, option: any) => {
    try {
      this.setState({ isDropdownOpen: false });
      let productDeptId: any;
      let resultString;
      if (this.state.DivdeptOption.length == 0) {
        let filteredData = this.deptId.filter((item) => item !== 0);
        resultString = filteredData.join(',');
      } else {
        resultString = this.state.DivdeptOption.map((divDeptText: string) => {
          const division = this.state.deptOption.find((e: any) => e.text === divDeptText);
          return division ? division.key : null;
        });
      }
      if (this.state.productDeptId1.length != 0 && this.state.productDeptId1 != null) {
        const deptkey = this.state.productDeptId1.map((DeptText: string) => {
          const depetname = this.state.departmentOptionItemcode.find(
            (e: any) => e.text === DeptText,
          );
          return depetname ? depetname.key : null;
        });
        productDeptId = deptkey.join(',');
      }
      //downloadAllItemCodeTemplate
      if (option.text == 'Export Itemcode Data') {
        const fromDate = this.state.downloadFromDate
          ? encodeURIComponent(this.state.downloadFromDate.toLocaleDateString('en-CA'))
          : undefined;
        const toDate = this.state.downloadToDate
          ? encodeURIComponent(this.state.downloadToDate.toLocaleDateString('en-CA'))
          : undefined;
        const status = 1;
        const token = this.token;
        let url = `${baseUrl}/api/Template/DownloadItemCodeTemplate`;
        url += `?${Object.entries({ token })
          .map(([key, value]) => `${key}=${value}`)
          .join('&')}`;
        if (fromDate) {
          url += `&fromDate=${fromDate}`;
        }
        if (toDate) {
          url += `&toDate=${toDate}`;
        }
        if (status) {
          url += `&status=${status}`;
        }
        if (productDeptId) {
          url += `&deptCode=${productDeptId}`;
        }
        if (resultString) {
          url += `&division=${resultString}`;
        }
        downloadFileItemCodeReady(url);
      } else {
        const fromDate = this.state.downloadFromDate
          ? encodeURIComponent(this.state.downloadFromDate.toLocaleDateString('en-CA'))
          : undefined;
        const toDate = this.state.downloadToDate
          ? encodeURIComponent(this.state.downloadToDate.toLocaleDateString('en-CA'))
          : undefined;
        const status = this.state.statusOptions == undefined || null ? 0 : this.state.statusOptions;
        // const department =
        //   this.state.downloadProductDept == 0 ? null : this.state.downloadProductDept;
        const token = this.token;
        let url = `${baseUrl}/api/Template/DownloadAllItemCodeTemplate`;
        url += `?${Object.entries({ token })
          .map(([key, value]) => `${key}=${value}`)
          .join('&')}`;
        if (fromDate) {
          url += `&fromDate=${fromDate}`;
        }
        if (toDate) {
          url += `&toDate=${toDate}`;
        }
        if (status == 0 || status) {
          url += `&status=${status}`;
        }
        if (productDeptId) {
          url += `&deptCode=${productDeptId}`;
        }
        if (resultString) {
          url += `&division=${resultString}`;
        }
        downloadFileItemCodeAll(url);
      }
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
  };

  onSearch(e: any) {
    var newValue = e?.target.value;
    newValue = newValue == undefined ? '' : newValue;
    this.setState({ searchValue: newValue, searchKey: newValue });
    var result = this.state.searchData.filter((element: any) => {
      return element?.uid
        ?.toLowerCase()
        .includes(newValue.trim().split(/ +/).join(' ').toLowerCase());
    });
    this.setState({ uidData: result });
    this.setState({
      uidData: result?.map((item: any, index: any) => ({ index: index + 1, ...item })),
    });
  }
  render() {
    const { t } = this.props;
    const { isDropdownOpen, hoveredOption } = this.state;
    const allCategorySelected =
      this.state.departmentOptionDP && this.state.departmentOptionDP.length
        ? this.state.productDeptId1.length === this.state.departmentOptionDP.length
        : false;
    const allDivSelected =
      this.state.deptOption && this.state.deptOption.length
        ? this.state.DivdeptOption &&
          this.state.DivdeptOption.length === this.state.deptOption.length
        : false;
    const columns = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{`${t('S_NO')}`}</div>,
        selector: (row: any) => row.index,
        isVisible: true,
        width: '100px',
      },
      {
        key: 'uid',
        name: <div className={columnHeader}>{`${t('COL_UID')}`}</div>,
        selector: (row: any) => <span title={row.uid}>{row.uid}</span>,
        isVisible: true,
        width: '160px',
      },
      {
        key: 'col_divCode',
        name: <div className={columnHeader}>{`${t('DIVISION')}`}</div>,
        selector: (row: any) => <span title={row.div_Name}>{row.div_Name}</span>,
        isVisible: true,
        width: '160px',
      },
      {
        key: 'dept_code',
        name: <div className={columnHeader}>{`${t('COL_DEPARTMENT')}`}</div>,
        selector: (row: any) => <span title={row.dept_Name}>{row.dept_Name}</span>,
        isVisible: true,
        width: '230px',
      },
      {
        key: 'date_created',
        name: <div className={columnHeader}>{`${t('COL_DATE_CREATED')}`}</div>,
        selector: (row: any) => (
          <span
            title={
              row.dateCreated != null && row.dateCreated != ''
                ? new Date(row.dateCreated).toLocaleDateString('en-GB')
                : ''
            }
          >
            {row.dateCreated != null && row.dateCreated != ''
              ? new Date(row.dateCreated).toLocaleDateString('en-GB')
              : ''}
          </span>
        ),
        isVisible: true,
        width: '160px',
      },
      {
        key: 'status',
        name: <div className={columnHeader}>{`${t('COL_STATUS')}`}</div>,
        selector: (row: any) => {
          const statusText = this.state.uidData.length !== 0 ? (row.status == 1 ? t('READY') : t('NOT_READY')) : '';
          return typeof statusText === 'string' ? (
          <span title={statusText}>{statusText}</span>
          ) : statusText;
        },
        isVisible: true,
        minWidth: '110px',
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
        <div className="bg-Color">
          <Stack>
            {/* <Text block className="text">{`${t('HEADER_PIM')} / ${t('ITEMCODE')}`}</Text> */}
            <Grid container spacing={1} direction="row" alignItems="center" className="grid">
              <Grid item lg={10.6} xs={10}>
                <Text variant="xxLarge" styles={PIMHearderText} className="color-blue text">
                  {`${t('ITEMCODE')}`}{' '}
                </Text>
              </Grid>
              <Grid item lg={0.9} xs={0} />
              {/* <Grid item lg={0.5} xs={1.6}>
                <Icon
                  iconName="Download"
                  className="iconStyle"
                  title={this.props.t('BTN_DOWNLOAD')}
                  onClick={() => {
                    if (this.state.searchData !== 0 && this.state.uidData.length !== 0) {
                      this.ItemcodeTemplate();
                    }
                  }}
                  // onClick={this.toggleDropdown}
                />
              </Grid> */}
              <Grid item lg={0.5} xs={1}>
                <Icon
                  iconName="Download"
                  className="iconStyle"
                  title={this.props.t('BTN_DOWNLOAD')}
                  onClick={this.toggleDropdown}
                />
                {isDropdownOpen && (
                  <div
                    className={mergeStyles('dropdown-container', {
                      position: 'absolute',
                      top: '29px',
                      right: '40px',
                      padding: '5px 0px',
                      zIndex: 9999,
                      backgroundColor: 'rgb(210, 238, 234)',
                      borderRadius: '5px',
                      textAlign: 'left',
                    })}
                    onMouseEnter={this.handleMouseEnter}
                    onMouseLeave={this.handleMouseLeave}
                    style={{
                      height: 'auto',
                      padding: '5px 0px',
                    }}
                  >
                    <DefaultButton
                      onClick={() => {
                        this.ItemcodeTemplate(null, { text: 'Export Itemcode Current View' });
                        this.setState({
                          hoveredOption: null,
                          isDropdownOpen: false,
                          hoveredOption1: false,
                        });
                      }}
                      disabled={this.state.Isdata === false}
                      style={{
                        fontFamily: 'Montserrat, sans-serif',
                        padding: '1px 0px 19px',
                        fontSize: '14px',
                        cursor: 'pointer',
                        border: 'none',
                        height: '14px',
                        backgroundColor: this.state.hoveredOption1 ? 'lightgray' : 'transparent',
                        display: 'block',
                        width: '100%',
                        fontWeight: 'bold',
                      }}
                      onMouseEnter={() => {
                        this.setState({ hoveredOption1: true });
                      }}
                      onMouseLeave={() => {
                        this.setState({ hoveredOption1: false });
                      }}
                    >
                      <span className="itmcodespan"> Export Itemcode Current View </span>
                    </DefaultButton>
                    <DefaultButton
                      onClick={() => {
                        this.ItemcodeTemplate(null, { text: 'Export Itemcode Data' });
                        this.setState({
                          hoveredOption: null,
                          isDropdownOpen: false,
                          hoveredOption2: false,
                        });
                      }}
                      disabled={this.state.downloadenable === false}
                      style={{
                        fontFamily: 'Montserrat, sans-serif',
                        padding: '0px 0px 22px',
                        fontSize: '14px',
                        cursor: 'pointer',
                        border: 'none',
                        height: '1px',
                        backgroundColor: this.state.hoveredOption2 ? 'lightgray' : 'transparent',
                        display: 'block',
                        width: '100%',
                        fontWeight: 'bold',
                      }}
                      onMouseEnter={() => {
                        this.setState({ hoveredOption2: true });
                      }}
                      onMouseLeave={() => {
                        this.setState({ hoveredOption2: false });
                      }}
                    >
                      <span className="itmcodespanbutton"> Export Itemcode Data</span>
                    </DefaultButton>
                  </div>
                )}
              </Grid>
            </Grid>
          </Stack>
          <Grid lg={12} item container spacing={1.5} direction={'row'}>
            <Grid item lg={2} xs={12}>
              <SearchBox
                className="searchBox"
                styles={PIMsearchBoxStyles}
                placeholder={`${t('UID_SEARCH')}`}
                onChange={(e: any) => this.onSearch(e)}
                value={this.state.searchKey}
                onClear={() => this.setState({ searchKey: '' })}
              />
            </Grid>
            <Grid item lg={1.98} xs={6}>
              <ComboBox
                className="dropdown"
                options={[
                  { key: 'select-toggle', text: 'Select All', selected: allDivSelected },
                  ...(this.state.deptOption || []).map((option: { text: any }) => ({
                    ...option,
                    selected: Array.isArray(this.state.DivdeptOption)
                      ? this.state.DivdeptOption.includes(option.text)
                      : false,
                  })),
                ]}
                styles={PIMdropdownStyles2}
                key={this.state.refreshKey1}
                multiSelect
                selectedKey={this.state.DivdeptOption}
                onChange={(e: any, selectedOption: any) => {
                  this.setState({
                    productDeptId1: [],
                    searchKey: '',
                    refreshKey: this.state.refreshKey + 1,
                    previousStatusOptions: 0,
                  });
                  if (selectedOption && selectedOption.key === 'select-toggle') {
                    const allSelected =
                      (this.state.DivdeptOption || []).length ===
                      (this.state.deptOption || []).length;
                    const newSelectedSeasons = allSelected
                      ? []
                      : (this.state.deptOption || [])
                          .map((option: any) => (option.text === 'Select All' ? null : option.text))
                          .filter(Boolean);
                    this.setState({ DivdeptOption: newSelectedSeasons, statusOptions: 0 }, () => {
                      this.DropdownFilter();
                    });
                  } else {
                    const updatedSelectedSeasons = Array.isArray(this.state.DivdeptOption)
                      ? [...this.state.DivdeptOption]
                      : [];
                    const isSelected = Array.isArray(this.state.DivdeptOption)
                      ? this.state.DivdeptOption.includes(selectedOption.text)
                      : false;
                    if (isSelected) {
                      const index = updatedSelectedSeasons.indexOf(selectedOption.text);
                      updatedSelectedSeasons.splice(index, 1);
                    } else {
                      updatedSelectedSeasons.push(selectedOption.text);
                    }
                    this.setState(
                      {
                        DivdeptOption: updatedSelectedSeasons,
                      },
                      () => {
                        this.DropdownFilter();
                      },
                    );
                  }
                }}
                placeholder={this.props.t('PLANOGRAM_TABLE_DIVISION')}
                // style={{ marginTop: -2 }}
              />
            </Grid>
            <Grid item lg={1.9} xs={6}>
              <ComboBox
                className="dropdown"
                multiSelect
                disabled={this.state.DivdeptOption.length == 0}
                options={[
                  { key: 'select-toggle', text: 'Select All', selected: allCategorySelected },
                  ...(this.state.departmentOptionDP || []).map((option: { text: any }) => ({
                    ...option,
                    selected: Array.isArray(this.state.productDeptId1)
                      ? this.state.productDeptId1.includes(option.text)
                      : false,
                  })),
                ]}
                key={this.state.refreshKey}
                styles={productDropdown}
                placeholder={`${t('DROPDOWN_SELECT_CATEGORY')}`}
                selectedKey={this.state.productDeptId1 || []}
                onChange={(e: any, selectedOption: any) => {
                  if (selectedOption && selectedOption.key === 'select-toggle') {
                    const allSelected =
                      (this.state.productDeptId1 || []).length ===
                      (this.state.departmentOptionDP || []).length;
                    const newSelectedSeasons = allSelected
                      ? []
                      : (this.state.departmentOptionDP || [])
                          .map((option: any) => (option.text === 'Select All' ? null : option.text))
                          .filter(Boolean);
                    this.setState({ productDeptId1: newSelectedSeasons }, () => {});
                  } else {
                    const updatedSelectedSeasons = Array.isArray(this.state.productDeptId1)
                      ? [...this.state.productDeptId1]
                      : [];
                    const isSelected = Array.isArray(this.state.productDeptId1)
                      ? this.state.productDeptId1.includes(selectedOption.text)
                      : false;
                    if (isSelected) {
                      const index = updatedSelectedSeasons.indexOf(selectedOption.text);
                      updatedSelectedSeasons.splice(index, 1);
                    } else {
                      updatedSelectedSeasons.push(selectedOption.text);
                    }
                    this.setState(
                      {
                        productDeptId1: updatedSelectedSeasons,
                      },
                      () => {},
                    );
                  }
                }}
              />
            </Grid>
            <Grid item lg={2.1} xs={6}>
              <CommonDate
                onMinDateChange={(e: any) => this.handleMinDateChange(e)}
                onToDateChange={(e: any) => this.handleToDateChange(e)}
                label={`${t('UIDCREATION_DATE')}`}
                ref={this.commonDateRef}
              />
            </Grid>
            {/* <Grid item lg={1.7} xs={6}>
              <Dropdown
                className="dropdown"
                placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                options={this.state.departmentOption}
                styles={productDropdown}
                onRenderOption={renderOption}
                defaultSelectedKey={this.state.productDeptId}
                selectedKey={this.state.productDeptId}
                onChanged={(selectedOption: IDropdownOption) => {
                  this.setState({ productDeptId: selectedOption.key });
                }}
              />
            </Grid> */}
            <Grid item lg={1.3} xs={6}>
              <Dropdown
                className="dropdown"
                placeholder={`${t('DROPDOWN_PLACEHOLDER_STATUS')}`}
                options={this.statusOptions}
                styles={productDropdown}
                defaultSelectedKey={this.state.statusOptions}
                selectedKey={this.state.statusOptions}
                onChanged={(selectedOption: IDropdownOption) => {
                  if (selectedOption.key !== 0) {
                    this.setState({
                      previousStatusOptions: selectedOption.key,
                    });
                  }
                  this.setState({ statusOptions: selectedOption.key, searchKey: '' });
                }}
              />
            </Grid>
            <Grid item lg={1.18} xs={0} />
            <Grid item lg={0.5} xs={1.6} spacing={0.8}>
              <Icon
                iconName="Search"
                title={t('BTN_SEARCH')}
                className="iconStyle"
                onClick={() => {
                  if (this.state.statusOptions === 0) {
                    this.setState({
                      previousStatusOptions: this.state.statusOptions,
                    });
                  }
                  if (
                    this.state.DivdeptOption.length != 0 ||
                    (this.state.statusOptions.length != 0 &&
                      this.state.previousStatusOptions != 0) ||
                    this.state.fromDate != null ||
                    this.state.toDate != null
                  ) {
                    this.searchProducts(
                      this.state.productDeptId1,
                      this.state.statusOptions,
                      this.state.fromDate,
                      this.state.toDate,
                    );
                  }
                  this.setState({ downloadenable: true });
                }}
              />
            </Grid>
            <Grid item lg={0.5} xs={1.6}>
              <Icon
                iconName="Clear"
                title={t('BTN_CLEAR')}
                className="iconStyle"
                onClick={() => {
                  if (
                    this.state.DivdeptOption.length != 0 ||
                    (this.state.statusOptions.length != 0 && this.state.statusOptions != 0) ||
                    this.state.fromDate != null ||
                    this.state.toDate != null ||
                    this.state.searchKey != ''
                  ) {
                    this.setState({
                      DivdeptOption: [],
                      productDeptId1: [],
                      statusOptions: 0,
                      fromDate: null,
                      toDate: null,
                      searchKey: '',
                    });
                    this.refresh();
                    // this.searchProducts(0, 0, null, null);
                    this.ClearDates();
                  }
                }}
              />
            </Grid>
            <Grid item lg={0.5} xs={1.6}>
              <Icon
                iconName="Refresh"
                title={t('BTN_REFRESH')}
                className="iconStyle"
                onClick={() => {
                  {
                    this.setState({
                      DivdeptOption: [],
                      productDeptId: 0,
                      productDeptId1: [],
                      statusOptions: 0,
                      fromDate: null,
                      toDate: null,
                    });
                    this.refresh();
                    this.ClearDates();
                  }
                }}
              />
            </Grid>
          </Grid>
          <div className="dataTableFixedFooter">
          <DataTable
            columns={columns}
            data={this.state.uidData}
            onColumnOrderChange={handleColumnReorder}
            pagination={true}
            selectableRowsHighlight
            highlightOnHover
            responsive
            fixedHeaderScrollHeight="68.2vh"
            fixedHeader
            striped
            paginationComponentOptions={{ rowsPerPageText: `${this.props.t('ROWS_PER_PAGE')}` }}
            paginationPerPage={this.Defaultperpage()}
            paginationRowsPerPageOptions={this.perpage()}
            noDataComponent={
              <div className="noDataWidth">
                {<DataTable columns={columns} data={[{ '': '' }]} />}
                <Stack className="noRecordsWidth">
                  {this.state.isRecord == true ? `${t('RECORDS')}` : `${t('NO_RECORDS')}`}
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

const ComponentTranslated = withTranslation()(ItemCode);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
