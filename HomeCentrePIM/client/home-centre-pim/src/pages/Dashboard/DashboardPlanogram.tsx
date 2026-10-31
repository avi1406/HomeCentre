import { DefaultButton, IComboBoxOption, Stack, ComboBox, Icon, IStyleFunctionOrObject } from '@fluentui/react';
import React, { Suspense, useEffect, useState } from 'react';
import { Text } from '@fluentui/react/lib/Text';
import { Grid } from '@mui/material';
import ApiManager from '@/services/ApiManager';
import AuthManagerService from '@/services/AuthManagerService';
import { withTranslation } from 'react-i18next';
import jwt_decode from 'jwt-decode';
import NotificationManager from '@/services/NotificationManager';
import axios from 'axios';

import { PieChart } from '@fluentui/react-charting';
import { DataVizPalette, ILegend, Legends, getColorFromToken } from '@fluentui/react-charting';
import DataTable from 'react-data-table-component';
import Util from '@/services/Util';
import CommonDate, { CommonDateRef } from '@/pages/commonFunctions';

import { ExportDivision, GetPlanoModalDto, PlanoCountModel } from '@/services/ApiClient';
import { baseUrl } from '@/services/Constants';
import {
  columndata,
  columnHeader,
  PIMHearderText,
  PIMHeaderButtonsQc,
  PIMButtonsQc,
  PIMdropdownStylesDd,
  expandableTable,
} from '@/pages/PimStyles';
import DashboardUid from './DashboardUid';
import { IPieChartStyleProps, IPieChartStyles } from '@fluentui/react-charting';

interface PlanoExpandableRowComponentProps {
  row: any;
}

interface PlanoExpandableRowComponentState {
  datas: any[] | undefined;
  data: string;
  loading: boolean;
  divisionId: any;
}

class DashboardPlanogram extends React.Component<any, any> {
  location = window.location.pathname;
  notify = new NotificationManager();
  apiClient = new ApiManager().CreateApiClient();
  utils = new Util();
  authManager = new AuthManagerService();
  isAuthenticated = this.authManager.isAuthenticated();
  user = this.isAuthenticated ? this.authManager.getUserData() : null;
  roleId: any[] = this.user?.roleId || [];
  deptId: any[] = this.user?.departmentId || [];
  token = sessionStorage.getItem('token');
  decode: any = jwt_decode(this.token || '');
  fieldType: IComboBoxOption[] | undefined;
  selectedRowData: null | undefined;
  rejected = false;
  commonDateRefs: React.RefObject<CommonDateRef>[] = Array.from({ length: 2 }, () =>
    React.createRef<CommonDateRef>(),
  );
  PTstatus: any = [];
  constructor(props: any) {
    super(props);
    this.state = {
      searchKey: '',
      selectedRowData: [],
      noSelection: false,
      expandflag: false,
      row: [],
      dropDownSelectedRecord: [],
      filteredRecord: [],
      selecteddept: [],
      showrecivedprd: true,
      Showrejecteditem: false,
      isRecord: false,
      exportvisible: false,
      rejecteddata: [],
      showActionColumn: false,
      A_D_Visible: false,
      recjected: false,
      handoverOptions1: [],
      dropdown: '',
      DropdownVal: 'All',
      refreshKey: 0,
      datas: undefined,
      loading: true,
      expansion: false,
      divisionId: [],
      selectedDivisionIds: [],
      expandedRowdata: null,
      expansionDivRecords: [],
      toDate: null,
      minDate: null,
    };
  }

  async componentDidMount(): Promise<void> {
    this.setState({
      isRecord: true,
    });
    this.refresh(this.state.minDate, this.state.todate, this.state.selectedDivisionIds);
    const filteredDeptId = this.deptId.filter((id) => id !== 0);
    var departmentList = (await this.apiClient.getDivision())?.result;
    var departmentOption: { key: number | undefined; text: string | undefined }[] = [];
    var deptOpt: { key: number | undefined; text: string | undefined }[] = [];

    // filteredDeptId.forEach((id) => {
    //   const matchingDepartment = departmentList?.find((dept) => dept.id === id);
    //   if (matchingDepartment) {
    //     departmentOption.push({ key: matchingDepartment.id, text: matchingDepartment.name });
    //     deptOpt.push({ key: matchingDepartment.id, text: matchingDepartment.name });
    //   }
    // });
    departmentList?.forEach((dept) => {
      const matchingId = filteredDeptId?.find((id) => id === dept.id);
      const matchingDepartment = departmentList?.find((dept) => dept.id === matchingId);
      if (matchingDepartment) {
        departmentOption.push({ key: matchingDepartment.id, text: matchingDepartment.name });
        deptOpt.push({ key: matchingDepartment.id, text: matchingDepartment.name });
      }
    });

    this.setState({
      departmentOption: departmentOption,
      deptOption: deptOpt,
      divisionorder: deptOpt,
      filteredDeptId: filteredDeptId,
      deptfilteredDeptId: filteredDeptId,
    });
  }

  // async refresh(mindate: any, todate: any, divisionIds: any[]) {
  //   this.setState({
  //     isRecord: true,
  //     downloadFromDate: null,
  //     downloadToDate: '',
  //   });
  //   try {
  //     this.setState({
  //       filteredRecord: [],
  //       selecteddept: [],
  //       selectedDivisionIds: [],
  //       selected: false,
  //       refreshKey: 0,
  //       // minDate: '',
  //       // toDate: '',
  //     });
  //     const minDate = this.state.mindate?.toLocaleDateString('en-CA') || '';
  //     const toDate = this.state.todate?.toLocaleDateString('en-CA') || '';
  //     const dashboardPlano = (await this.apiClient.getPlanoCount(minDate, toDate, divisionIds))
  //       .result;
  //     const filteredResponse = dashboardPlano?.filter((item: any) =>
  //       this.deptId.includes(item.division_code),
  //     );
  //     const response = filteredResponse?.map((item, index) => ({ index: index + 1, ...item }));
  //     const dashboardPlano1 = (await this.apiClient.getDivisionPlanoCount()).result;
  //     this.setState({
  //       rowData: response,
  //       rowData1: dashboardPlano1,
  //       copyRecord: response,
  //       filteredRecord: response,
  //     });
  //   } catch (e: any) {
  //     this.notify.showErrorNotify(e.message);
  //   }
  //   this.commonDateRefs.forEach((ref) => {
  //     if (ref.current) {
  //       ref.current.resetDates();
  //     }
  //   });
  //   this.setState({
  //     isRecord: false,
  //   });
  // }
  async refresh(mindate: any, todate: any, divisionIds: any[]) {
    this.setState({
      isRecord: true,
      downloadFromDate: null,
      downloadToDate: '',
      filteredRecord: [],
      selecteddept: [],
      selectedDivisionIds: [],
      selected: false,
      refreshKey: 0,
    });
  
    try {
      const minDate = mindate?.toLocaleDateString('en-CA') || ''; 
      const toDate = todate?.toLocaleDateString('en-CA') || ''; 
  
      const [dashboardPlanoResponse, dashboardPlano1Response] = await Promise.all([
        this.apiClient.getPlanoCount(minDate, toDate, divisionIds),
        this.apiClient.getDivisionPlanoCount(),
      ]);
  
      const dashboardPlano = dashboardPlanoResponse?.result || [];
      const filteredResponse = dashboardPlano.filter((item: any) =>
        this.deptId.includes(item.division_code),
      );
      const response = filteredResponse.map((item: any, index: number) => ({
        index: index + 1,
        ...item,
      }));
  
      const dashboardPlano1 = dashboardPlano1Response?.result || [];
  
      this.setState({
        rowData: response,
        rowData1: dashboardPlano1,
        copyRecord: response,
        filteredRecord: response,
      });
  
    } catch (e: any) {
      this.notify.showErrorNotify(e.message);
    }
  
    this.commonDateRefs.forEach((ref) => {
      if (ref.current) {
        ref.current.resetDates();
      }
    });
  
    this.setState({
      isRecord: false,
    });
  }  

  rowSelection = (e: any) => {
    const selectionCount = e.selectedCount;
    this.setState({ selectedRowData: e.selectedRows, selectionCount: selectionCount });
    switch (selectionCount) {
      case 0:
        this.setState({ enableEditButton: false, enableActiveButton: false, ApprovalEnable: true });
        break;
      case 1:
        this.setState({ enableEditButton: true, enableActiveButton: true, ApprovalEnable: true });
        break;
      default:
        this.setState({ enableEditButton: false, enableActiveButton: true });
        break;
    }
    if (selectionCount >= 2) {
      this.setState({ ApprovalEnable: false });
    }
  };

  handleMinDateChange = (date: any) => {
    this.setState({ minDate: date, downloadFromDate: date });
  };
  // perpage = () => {
  //   const recordCont = [25, 50, 100, 150, 200];
  //   return recordCont;
  // };
  // Defaultperpage = () => {
  //   const recordCont = 25;
  //   return recordCont;
  // };
  handleToDateChange = (date: any) => {
    this.setState({ toDate: date, downloadToDate: date });
  };
  resetDates = () => {
    this.setState({
      minDate: undefined,
      toDate: undefined,
    });
  };

  downloadFile = async (downloadFromDate: any, downloadToDate: any) => {
    if (downloadFromDate && downloadFromDate !== '') {
      var fromDate = downloadFromDate?.toLocaleDateString('en-CA') || '';
    }
    if (downloadToDate && downloadToDate !== '') {
      var toDate = downloadToDate?.toLocaleDateString('en-CA') || '';
    }
    const filteredDeptIdOrder = this.state.selectedDivisionIds.filter((id: any) => id !== 0);
    const desiredOrder = this.state.divisionorder.map((item: any) => item.key);
    const sortByDesiredOrder = (ids: any, order: any) => {
      return ids.slice().sort((a: any, b: any) => order.indexOf(a) - order.indexOf(b));
    };
    const filteredDeptId = sortByDesiredOrder(filteredDeptIdOrder, desiredOrder);

    this.notify.showSuccessNotify1(this.props.t('DOWNLOAD_STARTED'));
    document.body.classList.add('loading-indicator');

    let finalDeptId = filteredDeptId;
    if (this.state.expansion) {
      let ids = [61, 4, 65];
      const dataSet = ids.map((dId: any) => {
        const hasId = this.state.expansionDivRecords?.includes(dId);
        const edData = new ExportDivision();
        edData.divisionId = dId;
        edData.status = hasId;
        return edData;
      });
      console.log(dataSet);
      finalDeptId = dataSet;
    } else {
      if (finalDeptId?.length > 0) {
        finalDeptId = finalDeptId.map((e: any) => {
          const edData = new ExportDivision();
          edData.divisionId = e;
          edData.status = false;
          return edData;
        });
      }
    }
    if (filteredDeptId?.length > 0) {
      finalDeptId = finalDeptId.filter((f: any) => filteredDeptId?.includes(f?.divisionId));
    }

    const requestBody = new GetPlanoModalDto();
    requestBody.fromDate = fromDate || undefined;
    requestBody.toDate = toDate || undefined;
    requestBody.data = finalDeptId;

    // var url_ = `${baseUrl}/api/DashBoard/GetPlanoCountExcel?`;

    // if (!(fromDate === null || fromDate === '')) {
    //   url_ += 'fromDate=' + encodeURIComponent('' + fromDate) + '&';
    // }
    // if (!(toDate === null || toDate === '')) {
    //   url_ += 'toDate=' + encodeURIComponent('' + toDate) + '&';
    // }

    // if (finalDeptId && finalDeptId.length > 0) {
    //   finalDeptId.forEach((item: any) => {
    //     url_ += 'divisionId=' + encodeURIComponent('' + item) + '&';
    //   });
    // }

    // url_ = url_.replace(/[?&]$/, '');
    const url_ = `${baseUrl}/api/DashBoard/GetPlanoCountExcel`;

    axios({
      url: url_,
      method: 'POST',
      responseType: 'blob',
      data: requestBody,
      headers: {
        Token: this.token,
        'Content-Type': 'application/json;',
      },
    })
      .then((response) => {
        document.body.classList.remove('loading-indicator');
        this.notify.hideErrorNotify();
        this.notify.showSuccessNotify(this.props.t('DOWNLOAD_COMPLETED'));
        const blob: any = new Blob([response.data], { type: 'application/octet-stream' });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.setAttribute('download', 'PlanoCountExcel.xlsx');
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
      })
      .catch((error) => {
        document.body.classList.remove('loading-indicator');
        this.notify.showErrorNotify(error);
      });
  };


  Filterdata = () => {
    if (this.state.selecteddept.length != 0) {
      var selectedDeptname = this.state.selecteddept.map((selectedDept: any) => selectedDept);
      if (selectedDeptname.length !== this.state.deptOption.length) {
        var ids: any = [];
        var id;
        selectedDeptname.forEach((selectedDept: any) => {
          var option = this.state.deptOption.find((option: any) => {
            return selectedDept === option.text;
          });
          if (option) {
            id = option;
            ids.push(option.key);
          }
        });
        if (
          ids.length > 0 &&
          Array.isArray(this.state.filteredRecord) &&
          this.state.filteredRecord.length > 0
        ) {
          var record = this.state.filteredRecord.filter((e: any) => {
            return ids.includes(e.division_code);
          });

          this.setState({ filteredRecord: record, Dropdownvalue: id });
        }
      } else {
        this.setState({ filteredRecord: this.state.rowData });
      }
    } else {
      this.setState({ filteredRecord: this.state.rowData });
    }
  };

  async onSearchClick(minDate: any = '', toDate: any = '', divisionIds: any[]) {
    this.setState({
      isRecord: true,
    });

    if (minDate) {
      try {
        minDate = new Date(minDate).toLocaleDateString('en-CA');
      } catch (error) {
        console.error("Invalid minDate:", error);
        minDate = undefined;
      }
    } else {
      minDate = undefined;
    }
    if (toDate) {
      try {
        toDate = new Date(toDate).toLocaleDateString('en-CA');
      } catch (error) {
        console.error("Invalid toDate:", error);
        toDate = undefined;
      }
    } else {
      toDate = undefined;
    }
    if (!Array.isArray(divisionIds)) {
      divisionIds = [divisionIds];
    }
    const desiredOrder = this.state.divisionorder.map((item: any) => item.key);
    const sortByDesiredOrder = (ids: any, order: any) => {
      return ids.slice().sort((a: any, b: any) => order.indexOf(a) - order.indexOf(b));
    };
    const division = sortByDesiredOrder(divisionIds, desiredOrder);
    const response = (await this.apiClient.getPlanoCount(minDate, toDate, division)).result;
    const filteredResponse = response?.filter((item: any) =>
      this.deptId.includes(item.division_code),
    );
    const order = (items: any[], order: any[]) => {
      return items.slice().sort((a, b) => {
        if (a.division_code === 0) return 1;
        if (b.division_code === 0) return -1;
        return order.indexOf(a.division_code) - order.indexOf(b.division_code);
      });
    };
    // const responeWithIndex = filteredResponse?.map((item: any, index: any) => ({
    //   index: index + 1,
    //   ...item,
    // }));
    let sortedFilteredResponse: any;
    if (filteredResponse && filteredResponse.length > 0) {
      sortedFilteredResponse = order(filteredResponse, desiredOrder);
      if (this.state.expandedRowdata) {
        this.setState({
          expandedRowdata: sortedFilteredResponse[0].division_code,
        });
      }
    } else {
      this.setState({
        expandedRowdata: null,
      });
    }

    this.setState(
      {
        searchKey: '',
        onClickMinDate: minDate,
        onClickToDate: toDate,
        rowData: sortedFilteredResponse,
        dropDownSelectedRecord: sortedFilteredResponse,
        filteredRecord: sortedFilteredResponse,
        isRecord: false,
        exportvisible: sortedFilteredResponse?.length !== 0 ? true : false,
      },
      // () => {
      //   this.Filterdata();
      // },
    );
  }

  expandableRowsComponent = () => {
    if (this.state.expandedRowdata !== 0) {
      return <this.PlanoExpandableRowComponent row={this.state.rowData} />;
    } else {
      return <></>;
    }
  };

  handleRowExpand = (row: any, expanded: any) => {
    if (expanded) {
      this.setState({ expandedRowdata: expanded.division_code });
    }
    if (row) {
      this.setState({ expansionDivRecords: [...this.state.expansionDivRecords, expanded.division_code] });
    } else {
      const filteredRecords: any = this.state.expansionDivRecords?.filter((f: any) => f != expanded.division_code)
      this.setState({ expansionDivRecords: filteredRecords });
      if (filteredRecords?.length === 0) {
        this.setState({ expansion: false });
      }
    }
  };

  handleRowHide = (row: any) => {
    if (row.division_code === 0) {
      return true;
    }
    return false;
  };

  PlanoExpandableRowComponent: React.FC<PlanoExpandableRowComponentProps> = (props: {
    row: any;
  }) => {
    const [datas, setDatas] = useState<PlanoExpandableRowComponentState['datas']>(undefined);
    const [loading, setLoading] = useState<PlanoExpandableRowComponentState['loading']>(true);
    //const [table, setTable] = useState<any>(undefined);

    const apiClient = new ApiManager().CreateApiClient();

    useEffect(() => {
      const fetchData = async () => {
        try {
          this.setState({ expansion: true });
          const divisionId = this.state.expandedRowdata;
          const minDate = this.state.minDate instanceof Date 
  ? this.state.minDate.toLocaleDateString('en-CA') 
  : undefined;

const toDate = this.state.toDate instanceof Date 
  ? this.state.toDate.toLocaleDateString('en-CA') 
  : undefined;

          const dashboardPlanoexpand = await apiClient.getPlanoUserCount(
            divisionId,
            minDate,
            toDate,
          );
          setDatas(
            dashboardPlanoexpand.result?.map((item, index) => ({
              index: index + 1,
              columnName: (
                <table
                  className={expandableTable.table}
                  style={{
                    backgroundColor:
                      divisionId === 61 || divisionId === 65
                        ? index % 2 === 0
                          ? 'rgb(240, 255, 255)'
                          : 'rgb(201, 220, 237)'
                        : divisionId === 4
                          ? index % 2 === 0
                            ? 'rgb(201, 220, 237)'
                            : 'rgb(240, 255, 255)'
                          : index % 2 === 0
                            ? 'rgb(240, 255, 255)'
                            : 'rgb(201, 220, 237)',
                  }}
                >
                  <tbody>
                    <tr className={expandableTable.row}>
                      <td className={`${expandableTable.expandIconCol} stylecc`}></td>
                      <td className={`${expandableTable.planoDivCol} stylec`}>{item.name ?? ''}</td>
                      <td className={expandableTable.countCols}>
                        {item.planogram_Total_Count ?? ''}
                      </td>
                      <td className={expandableTable.countCols}>
                        {item.planogram_Approved_Count ?? ''}
                      </td>
                      <td className={expandableTable.countCols}>
                        {item.planogram_Rejected_Count ?? ''}
                      </td>
                      <td className={expandableTable.countCols}>
                        {item.planogram_Pending_Count ?? ''}
                      </td>
                      <td className={expandableTable.countCols}>{item.uid_Count ?? ''}</td>
                    </tr>
                  </tbody>
                </table>
              ),
              ...item,
            })) ?? [],
          );
          setLoading(false);
        } catch (error: any) {
          this.notify.showErrorNotify(error);
        }
      };
      fetchData();
    }, []);
    return (
      <div>
        {loading ? (
          <div>Loading...</div>
        ) : datas && datas.length > 0 ? (
          datas.map((item, index) => (
            <div key={index} style={{ minWidth: '80px' }}>
              <div className={columndata}>{item.columnName}</div>
            </div>
          ))
        ) : (
          // <div>No data available</div>
          <></>
        )}
      </div>
    );
  };
  calculatePercentages() {
    const datas = this.state.rowData1;
    if (!datas) return [];

    const totalCounts = {
      houseHold: 0,
      furniture: 0,
      cashAndCarry: 0,
    };

    datas.forEach((data: any) => {
      switch (data.division_code) {
        case 61:
          totalCounts.houseHold += data.total_Count;
          break;
        case 4:
          totalCounts.furniture += data.total_Count;
          break;
        case 65:
          totalCounts.cashAndCarry += data.total_Count;
          break;
        default:
          break;
      }
    });

    const total = totalCounts.houseHold + totalCounts.furniture + totalCounts.cashAndCarry;
    return [
      { y: parseFloat(((totalCounts.houseHold / total) * 100).toFixed(2)), x: 'House Hold', color: '#9254de' },
      { y: parseFloat(((totalCounts.furniture / total) * 100).toFixed(2)), x: 'Furniture', color: '#0078D4' },
      { y: parseFloat(((totalCounts.cashAndCarry / total) * 100).toFixed(2)), x: 'Cash And Carry', color: '#0078D4' },
    ];
  }

  render() {
    const points = this.calculatePercentages();
    const validPoints = points.filter((point) => !isNaN(point.y) && point.y !== 0);
    const legends: ILegend[] = validPoints.map((point, index) => {
      const titles = ['House Hold', 'Furniture', 'Cash And Carry'];
      const colors = ['#0075a4', '#72adcf', '#bfe8ff'];

      return {
        // title: `${titles[index]} (${point.y}%)`,
        title: <span style={{ fontWeight: 'bold' }}>{`${titles[index]} (${point.y}%)`}</span> as any,
        color: getColorFromToken(colors[index]),
        className: 'legend',
        action: () => { },
        hoverAction: () => { },
        onMouseOutAction: () => { },
      };
    });
    const { t } = this.props;
    const { deptOption } = this.state;
    const selecteddept =
      deptOption && deptOption.length
        ? this.state.selecteddept.length === deptOption.length
        : false;
    var _columns: any = [
      {
        key: 'divison',
        name: <div className={columnHeader}>{`${this.props.t('DIVISION')}`}</div>,
        selector: (row: any) => {
          switch (row.division_code) {
            case 61:
              return 'HC-House Hold';
            case 4:
              return 'HC-Furniture';
            case 65:
              return 'Homecentre Daily';
            case 0:
              return 'Total';
            case null:
              return 'Total';
            default:
              return null;
          }
        },
        isVisible: true,
        minWidth: '105px',
      },
      {
        key: 'totalplanofamilycreated',
        name: (
          <div className={columnHeader}>{`${this.props.t('COL_TOTALPLANO_FAMILY_CREATED')}`}</div>
        ),
        selector: (row: any) => <span title={row.total_Count}>{row.total_Count}</span>,
        isVisible: true,
        minWidth: '100px',
      },
      {
        key: 'totalplanofamilypending',
        name: (
          <div className={columnHeader}>{`${this.props.t('COL_TOTALPLANO_FAMILY_APPROVED')}`}</div>
        ),
        selector: (row: any) => <span title={row.approved_Count}>{row.approved_Count}</span>,
        isVisible: true,
        minWidth: '100px',
      },
      {
        key: 'totalplanofamilyrejected',
        name: (
          <div className={columnHeader}>{`${this.props.t('COL_TOTALPLANO_FAMILY_REJECTED')}`}</div>
        ),
        selector: (row: any) => <span title={row.rejected_Count}>{row.rejected_Count}</span>,
        isVisible: true,
        minWidth: '100px',
      },
      {
        key: 'totalplanofamilyapprovalpending',
        name: (
          <div className={columnHeader}>{`${this.props.t(
            'COL_TOTALPLANO_FAMILY_APPROVAL_PENDING',
          )}`}</div>
        ),
        selector: (row: any) => <span title={row.pending_Count}>{row.pending_Count}</span>,
        isVisible: false,
        minWidth: '100px',
      },
      {
        key: 'totaluidgenerated',
        name: <div className={columnHeader}>{`${this.props.t('COL_TOTAL_UID_GENERATED')}`}</div>,
        selector: (row: any) => <span title={row.uid_Count}>{row.uid_Count}</span>,
        isVisible: true,
        minWidth: '100px',
      },
    ];
    const handleColumnReorder = (newColumns: any) => {
      var _column: any = [..._columns];
      if (newColumns == this.state.currentColumns) {
        this.setState({ currentColumns: _column });
      } else {
        this.setState({ currentColumns: newColumns });
      }
    };

    const customPieChartStyles: IStyleFunctionOrObject<IPieChartStyleProps, IPieChartStyles> = {
      chartTitle: {
        marginRight: '66px',
        fontWeight: 'bold'
      },
    }

    return (
      <div
        className="layout width-100"
        style={{ height: this.state.showActionColumn ? '89.25vh' : 'auto' }}
      >
        {this.state.showActionColumn == false && (
          <>
            <Stack>
              <Text variant="xxLarge" styles={PIMHearderText} className="color-blue text">{`${t(
                'COL_DASHBOARD_PLANOGRAM',
              )}`}</Text>
            </Stack>
          </>
        )}
        {this.state.showActionColumn == true && (
          <>
            <Stack>
              <Text variant="xxLarge" styles={PIMHearderText} className="color-blue text">{`${t(
                'COL_DASHBOARD_PLANOGRAM',
              )}`}</Text>
            </Stack>
          </>
        )}
        <div className="ReceivedClassProduct" />
        <Grid lg={12} item container spacing={0} direction={'row'}>
          {this.roleId.includes(1) || this.roleId.includes(2) ||
            this.roleId.includes(3) || this.roleId.includes(4) ||
            this.roleId.includes(5) || this.roleId.includes(7) ? (
            <Grid item lg={2.1} xs={5}>
              <DefaultButton
                className="button"
                text={`${t('PLANO_REPORT')}`}
                styles={this.state.showrecivedprd == true ? PIMHeaderButtonsQc : PIMButtonsQc}
                onClick={() => {
                  this.rejected = false;
                  this.setState(
                    {
                      showActionColumn: false,
                      showrecivedprd: true,
                      Showrejecteditem: false,
                      expandedRowdata: null,
                      minDate: '',
                      toDate: '',
                    },
                    () => {
                      this.refresh(null, null, []);
                    },
                  );
                }}
              />
            </Grid>
          ) : (
            ''
          )}
          {this.roleId.includes(1) || this.roleId.includes(2) ||
            this.roleId.includes(3) || this.roleId.includes(4) ||
            this.roleId.includes(5) || this.roleId.includes(7) ? (
            <Grid item lg={2.1} xs={5}>
              <DefaultButton
                className="Qcbutton"
                text={`${t('DASH_UID_REP')}`}
                styles={this.state.showActionColumn == true ? PIMHeaderButtonsQc : PIMButtonsQc}
                onClick={() => {
                  this.rejected = false;
                  this.setState({
                    showActionColumn: true,
                    Showrejecteditem: false,
                    showrecivedprd: false,
                  });
                }}
              />
            </Grid>
          ) : (
            ''
          )}
          <Grid item lg={this.roleId.includes(1) ? 7.43 : 7.43} xs={1} />
          {this.state.showActionColumn == false && (
            <Grid item lg={0.35} xs={2}>
              <Icon
                iconName="Download"
                className="iconStyle"
                title={this.props.t('BTN_DOWNLOAD')}
                onClick={() => {
                  if (this.state.rowData.length !== 0) {
                    this.downloadFile(this.state.downloadFromDate, this.state.downloadToDate);
                  }
                }}
              />
            </Grid>
          )}
        </Grid>
        {/* <br /> */}
        <Grid lg={12} item container spacing={1.5} paddingBottom={1} direction={'row'}>
          {this.state.showActionColumn == false ? (
            <Grid lg={12} item container spacing={1.5} direction={'row'}>
              <Grid item lg={1.9} xs={6}>
                <ComboBox
                  multiSelect
                  placeholder={this.props.t('PLANOGRAM_TABLE_DIVISION')}
                  styles={PIMdropdownStylesDd}
                  options={[
                    { key: 'select-toggle', text: 'Select All', selected: selecteddept },
                    ...(deptOption || []).map((option: any) => ({
                      key: option.key,
                      text: option.text,
                      selected: Array.isArray(this.state.selecteddept)
                        ? this.state.selecteddept.includes(option.text)
                        : false,
                    })),
                  ]}
                  key={this.state.refreshKey}
                  onChange={(e: any, selectedOption: any) => {
                    if (selectedOption && selectedOption.key === 'select-toggle') {
                      const allSelected =
                        (this.state.selecteddept || []).length === (deptOption || []).length;
                      const newSelectedSeasons = allSelected
                        ? []
                        : deptOption
                          .map((option: any) =>
                            option.text === 'Select All' ? null : option.text,
                          )
                          .filter(Boolean);
                      const newSelectedSeasons1 = allSelected
                        ? []
                        : deptOption
                          .map((option: any) =>
                            option.text === 'Select All' ? null : option.key,
                          )
                          .filter(Boolean);
                      this.setState({
                        selecteddept: newSelectedSeasons,
                        divisionSelected: '',
                        searchKey: '',
                        selectedDivisionIds: newSelectedSeasons1,
                      });
                    } else {
                      const updatedSelectedSeasons = Array.isArray(this.state.selecteddept)
                        ? [...this.state.selecteddept]
                        : [];
                      const updatedSelectedSeasons1 = Array.isArray(this.state.selectedDivisionIds)
                        ? [...this.state.selectedDivisionIds]
                        : [];
                      const isSelected = Array.isArray(this.state.selecteddept)
                        ? this.state.selecteddept.includes(selectedOption.text)
                        : false;
                      if (isSelected) {
                        const index = updatedSelectedSeasons.indexOf(selectedOption.text);
                        const index1 = updatedSelectedSeasons1.indexOf(selectedOption.key);
                        updatedSelectedSeasons.splice(index, 1);
                        updatedSelectedSeasons1.splice(index1, 1);
                      } else {
                        updatedSelectedSeasons.push(selectedOption.text);
                        updatedSelectedSeasons1.push(selectedOption.key);
                      }
                      this.setState({
                        selecteddept: updatedSelectedSeasons,
                        selectedDivisionIds: updatedSelectedSeasons1,
                        divisionSelected: '',
                        searchKey: '',
                      });
                    }
                  }}
                  selectedKey={this.state.selecteddept || []}
                />
              </Grid>

              <Grid item lg={2.15} xs={6}>
                <CommonDate
                  onMinDateChange={(e: any) => this.handleMinDateChange(e)}
                  onToDateChange={(e: any) => this.handleToDateChange(e)}
                  ref={this.commonDateRefs[0]}
                // label={`${t('RECEIVED_DATE')}`}
                />
                {/* placeholder={t('RECEIVED_DATE_PALCEHOLDER')} */}
              </Grid>
              <Grid item lg={6.45} xs={1} />
              <Grid item lg={0.5} xs={2}>
                <Icon
                  iconName="Search"
                  title={this.props.t('BTN_SEARCH')}
                  className="iconStyle"
                  onClick={() => {
                    if (
                      this.state.minDate ||
                      this.state.toDate ||
                      this.state.selectedDivisionIds.length > 0
                    ) {
                      this.setState({ filteredRecord: [], expandflag: true }, () => {
                        this.onSearchClick(
                          this.state.minDate,
                          this.state.toDate,
                          this.state.selectedDivisionIds,
                        );
                      });
                      //this.refresh(this.state.minDate, this.state.toDate, this.state.division_Id);
                    }
                  }}
                />
              </Grid>
              <Grid item lg={0.5} xs={1}>
                <Icon
                  iconName="Clear"
                  title={t('BTN_CLEAR')}
                  className="iconStyle"
                  onClick={() => {
                    if (
                      this.state.toDate ||
                      this.state.minDate ||
                      this.state.selectedDivisionIds.length > 0
                    ) {
                      this.setState(
                        {
                          downloadFromDate: null,
                          downloadToDate: '',
                          fromDate: null,
                          toDate: '',
                          minDate: '',
                          filteredRecord: [],
                          expandedRowdata: null,
                          expandflag: false,
                        },
                        () => {
                          this.resetDates();
                          this.refresh(null, null, []);
                        },
                      );
                    }
                  }}
                />
              </Grid>
              <Grid item lg={0.5} xs={2}>
                <Icon
                  iconName="Refresh"
                  title={this.props.t('BTN_REFRESH')}
                  className="iconStyle"
                  onClick={() => {
                    this.setState(
                      {
                        fromDate: null,
                        toDate: '',
                        minDate: '',
                        filteredRecord: [],
                        expandedRowdata: null,
                        expandflag: false,
                      },
                      () => {
                        this.refresh(null, null, []);
                      },
                    );
                  }}
                />
              </Grid>
            </Grid>
          ) : (
            ''
          )}
          {this.state.showActionColumn == true ? <DashboardUid /> : ''}
        </Grid>
        {this.state.showActionColumn === false && (
          <div className="tablegraph">
            {this.state.showActionColumn === false && this.rejected === false && (
              <>
                {this.state.expandflag === true ? (
                  <DataTable
                    className="planoDashboardDatatable dashboard"
                    columns={_columns}
                    data={this.state.filteredRecord}
                    expandableRows
                    expandableRowsComponent={this.expandableRowsComponent}
                    expandableRowDisabled={(row) => row.division_code === 0}
                    clearSelectedRows={this.state.noSelection}
                    onSelectedRowsChange={this.rowSelection}
                    onColumnOrderChange={handleColumnReorder}
                    onRowExpandToggled={this.handleRowExpand}
                    expandableRowExpanded={(row) =>
                      row.division_code === this.state.expandedRowdata
                    }
                    highlightOnHover
                    fixedHeader
                    fixedHeaderScrollHeight="61.8vh"
                    striped
                    noDataComponent={
                      <div className="noDataWidth">
                        <DataTable columns={_columns} data={[{ '': '' }]} />
                        <Stack className="noRecordsWidth">
                          {this.state.isRecord === true
                            ? `${this.props.t('RECORDS')}`
                            : `${this.props.t('NO_RECORDS')}`}
                        </Stack>
                      </div>
                    }
                  />
                ) : (
                  <DataTable
                    className="planoDashboardDatatable dashboard"
                    columns={_columns}
                    data={this.state.filteredRecord}
                    expandableRows
                    expandableRowsComponent={this.expandableRowsComponent}
                    expandableRowDisabled={(row) => row.division_code === 0}
                    clearSelectedRows={this.state.noSelection}
                    onSelectedRowsChange={this.rowSelection}
                    onColumnOrderChange={handleColumnReorder}
                    onRowExpandToggled={this.handleRowExpand}
                    highlightOnHover
                    fixedHeader
                    fixedHeaderScrollHeight="69.1vh"
                    striped
                    noDataComponent={
                      <div className="noDataWidth">
                        <DataTable columns={_columns} data={[{ '': '' }]} />
                        <Stack className="noRecordsWidth">
                          {this.state.isRecord === true
                            ? `${this.props.t('RECORDS')}`
                            : `${this.props.t('NO_RECORDS')}`}
                        </Stack>
                      </div>
                    }
                  />
                )}
              </>
            )}
            {(this.state.filteredRecord.length > 0 && this.state.showActionColumn === false) && (
              <div>
                <div className="chartdiv">
                  <PieChart
                    width={500}
                    height={220}
                    culture={window.navigator.language}
                    data={points}
                    chartTitle="Division Chart"
                    colors={['#0075a4', '#72adcf', '#bfe8ff']}
                    styles={customPieChartStyles}
                  />
                </div>
                {legends.length > 0 && <Legends legends={legends} className='legend' />}
              </div>
            )}
          </div>
        )}
      </div>
    );
  }
}

const ComponentTranslated = withTranslation()(DashboardPlanogram);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}
export default App;
