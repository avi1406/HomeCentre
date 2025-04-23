import React, { Suspense, useEffect, useState } from 'react';
import { withTranslation } from 'react-i18next';
import DataTable from 'react-data-table-component';
import { Icon, Stack, ComboBox, IStyleFunctionOrObject } from '@fluentui/react';
import { Grid } from '@mui/material';
import NotificationManager from '@/services/NotificationManager';
import ApiManager from '@/services/ApiManager';
import AuthManagerService from '@/services/AuthManagerService';
import jwt_decode from 'jwt-decode';
import { baseUrl } from '@/services/Constants';
import axios from 'axios';
import CommonDate, { CommonDateRef } from '@/pages/commonFunctions';
import {
  columnHeader,
  PIMdropdownStylesDd,
  PIMDropdownStyles1,
  columndata,
  expandableTable,
} from '@/pages/PimStyles';
import { PieChart } from '@fluentui/react-charting';
import { DataVizPalette, ILegend, Legends, getColorFromToken } from '@fluentui/react-charting';
import { IPieChartStyleProps, IPieChartStyles } from '@fluentui/react-charting';
import { ExportDept, GetTemplatesUidCountExcel } from '@/services/ApiClient';

interface UIDExpandableRowComponentProps {
  row: any;
}

interface UIDExpandableRowComponentState {
  datas: any[] | undefined;
  data: string;
  loading: boolean;
  divisionId: any;
}

class DashboardUid extends React.Component<any, any> {
  location = window.location.pathname;
  notify = new NotificationManager();
  apiClient = new ApiManager().CreateApiClient();
  authManager = new AuthManagerService();
  isAuthenticated = this.authManager.isAuthenticated();
  user = this.isAuthenticated ? this.authManager.getUserData() : null;
  roleId: any[] = this.user?.roleId || [];
  deptId: any[] = this.user?.departmentId || [];
  token = sessionStorage.getItem('token');
  decode: any = jwt_decode(this.token || '');
  commonDateRefs: React.RefObject<CommonDateRef>[] = Array.from({ length: 2 }, () =>
    React.createRef<CommonDateRef>(),
  );
  selectedDeptno1: any = [];
  selectedClassCode1: any = [];
  selectedSubClassCode1: any = [];
  constructor(props: any) {
    super(props);
    this.state = {
      isRecord: false,
      expansion: false,
      DashboardUid: [],
      searchData: [],
      selectedRowData: [],
      deptOption: [],
      divoption: [],
      selecteddept: [],
      noSelection: false,
      loading: true,
      fromDate: null,
      toDate: null,
      selecteddepartment: [],
      selectedclass: [],
      selectedsubclass: [],
      selectedDivisionIds: [],
      selectedDepartmentIds: [],
      selectedClassIds: [],
      selectedSubClassIds: [],
      expandedRowdata: null,
      expandflag: false,
      expandedRowdata1: null,
      expansionDivRecords: []
    };
  }
  commonDateRef = React.createRef<CommonDateRef>();
  async componentDidMount(): Promise<void> {
    this.setState({
      isRecord: true,
    });
    this.FilterFun();
    this.refresh();
  }
  // async refresh() {
  //   this.setState({
  //     isRecord: true,
  //   });
  //   var deptOpt: { key: number | undefined; text: string | undefined }[] = [];
  //   const filteredDeptId = this.deptId.filter((id) => id !== 0);
  //   var departmentList = (await this.apiClient.getDivision())?.result;
  //   // filteredDeptId.forEach((id) => {
  //   //   const matchingDepartment = departmentList?.find((dept) => dept.id === id);
  //   //   if (matchingDepartment) {
  //   //     deptOpt.push({
  //   //       key: matchingDepartment.id,
  //   //       text: matchingDepartment.name,
  //   //     });
  //   //   }
  //   // });
  //   departmentList?.forEach((dept) => {
  //     const matchingId = filteredDeptId?.find((id) => id === dept.id);
  //     const matchingDepartment = departmentList?.find((dept) => dept.id === matchingId);
  //     if (matchingDepartment) {
  //       deptOpt.push({ key: matchingDepartment.id, text: matchingDepartment.name });
  //     }
  //   });
  //   let Adddata: any[] = [];
  //   const minDate = this.state.fromDate?.toLocaleDateString('en-CA') || '';
  //   const toDate = this.state.toDate?.toLocaleDateString('en-CA') || '';
  //   var data = (
  //     await this.apiClient.getTemplatesUidCount(
  //       this.state.selectedDivisionIds,
  //       this.state.selectedDepartmentIds,
  //       this.state.selectedClassIds,
  //       this.state.selectedSubClassIds,
  //       minDate,
  //       toDate,
  //     )
  //   ).result;
  //   data?.forEach((e: any) => {
  //     var div = deptOpt.find((div: any) => {
  //       if (e.divisionCode != 'Total') {
  //         return div.key == e.divisionCode || div?.key?.toString() == e?.divisionCode?.toString();
  //       }
  //     });
  //     if (div || e?.divisionCode == 'Total') {
  //       Adddata.push({
  //         ...e,
  //         divisionName: div?.text || e.divisionCode,
  //       });
  //     }
  //   });
  //   var data1 = (await this.apiClient.getTemplatesUidDivisionCount()).result;
  //   this.setState({
  //     DashboardUid: Adddata,
  //     filteredRecord: Adddata,
  //     rowData1: data1,
  //     searchData: [],
  //     deptOption: deptOpt,
  //     divisionorder: deptOpt,
  //     fromDate: null,
  //     toDate: null,
  //     isRecord: false,
  //   });
  // }
  async refresh() {
    this.setState({ isRecord: true });
  
    const filteredDeptId = this.deptId.filter((id) => id !== 0);
    let deptOpt: { key: number | undefined; text: string | undefined }[] = [];
  
    try {
      const [departmentListResponse, dataResponse, data1Response] = await Promise.all([
        this.apiClient.getDivision(),
        this.apiClient.getTemplatesUidCount(
          this.state.selectedDivisionIds,
          this.state.selectedDepartmentIds,
          this.state.selectedClassIds,
          this.state.selectedSubClassIds,
          this.state.fromDate?.toLocaleDateString('en-CA') || '',
          this.state.toDate?.toLocaleDateString('en-CA') || ''
        ),
        this.apiClient.getTemplatesUidDivisionCount(),
      ]);
  
      const departmentList = departmentListResponse?.result;
      const data = dataResponse?.result;
      const data1 = data1Response?.result;
  
      deptOpt = departmentList
        ?.filter((dept) => filteredDeptId.includes(dept.id))
        .map((dept) => ({ key: dept.id, text: dept.name })) || [];
  
      let Adddata: any[] = [];
  
      data?.forEach((e: any) => {
        const div = deptOpt.find((div) =>
          e.divisionCode !== 'Total'
            ? div.key == e.divisionCode || div?.key?.toString() == e?.divisionCode?.toString()
            : undefined
        );
        Adddata.push({
          ...e,
          divisionName: div?.text || e.divisionCode,
        });
      });
  
      this.setState({
        DashboardUid: Adddata,
        filteredRecord: Adddata,
        rowData1: data1,
        searchData: [],
        deptOption: deptOpt,
        divisionorder: deptOpt,
        fromDate: null,
        toDate: null,
        isRecord: false,
      });
  
    } catch (e: any) {
      this.notify.showErrorNotify(e.message);
      this.setState({ isRecord: false });
    }
  }  

  async onSearchClick(selectedDivisionIds: any[]) {
    const minDate = this.state.fromDate?.toLocaleDateString('en-CA') || undefined;
    const toDate = this.state.toDate?.toLocaleDateString('en-CA') || undefined;
    let Adddata: any[] = [];
    const desiredOrder = this.state.divisionorder.map((item: any) => item.key);
    const sortByDesiredOrder = (ids: any, order: any) => {
      return ids.slice().sort((a: any, b: any) => order.indexOf(a) - order.indexOf(b));
    };
    const sortedDivisionIds = sortByDesiredOrder(selectedDivisionIds, desiredOrder);
    const divisionCodes = sortedDivisionIds?.join(',');
    const deptCodes = this.state.selectedDepartmentIds?.join(',');
    const classCodes = [...new Set(this.state.selectedClassIds)]?.join(',');
    const subclassCodes = [...new Set(this.state.selectedSubClassIds)]?.join(',');
    const response = (
      await this.apiClient.getTemplatesUidCount(
        divisionCodes,
        deptCodes,
        classCodes,
        subclassCodes,
        minDate,
        toDate,
      )
    ).result;
    response?.forEach((e: any) => {
      var div = this.state.deptOption.find((div: any) => {
        if (e.divisionCode != 'Total') {
          return div.key == e.divisionCode;
        }
      });
      Adddata.push({
        ...e,
        divisionName: div?.text || e.divisionCode,
      });
    });
    if (Adddata && Adddata.length > 0) {
      if (this.state.expandedRowdata) {
        this.setState({
          expandedRowdata: Adddata[0].division_code,
        });
      }
    } else {
      this.setState({
        expandedRowdata: null,
      });
    }
    //const filteredResponse = response?.filter((item: any) => this.deptId.includes(item.division_Id));
    // const responeWithIndex = filteredResponse?.map((item: any, index: any) => ({
    //   index: index + 1,
    //   ...item,
    // }));

    this.setState(
      {
        searchKey: '',
        onClickMinDate: minDate,
        onClickToDate: toDate,
        rowData: Adddata,
        dropDownSelectedRecord: Adddata,
        filteredRecord: Adddata,
        isRecord: false,
        exportvisible: Adddata?.length !== 0 ? true : false,
      },
      // () => {
      //   this.Filterdata();
      // },
    );
  }

  Adddivision = () => {
    if (this.state.selecteddept.length != 0) {
      const filteredData1 = this.state.deptOption.filter((item: any) => {
        return this.state.selecteddept.includes(item.text);
      });
      this.setState({ expandedRowdata: filteredData1[0].key }, () => {
        this.expandableRowsComponent();
      });
    }
  };
  async FilterFun() {
    const MasterData = (await this.apiClient.getHierarchicalMasters()).result;
    const departmentSet = new Set();
    const subClassSet = new Set();
    const classCodeSet = new Set();
    const departmentSet1 = new Set();
    const classCodeSet1 = new Set();
    const subClassSet1 = new Set();

    MasterData?.forEach((element) => {
      if (this.deptId.includes(element.division)) {
        departmentSet.add({ dept: element.dept, dept_Name: element.dept_Name });
        //departmentSet.add(element.dept_Name);
      }
      if (this.deptId.includes(element.division)) {
        classCodeSet.add({ class: element.class, class_Name: element.class_Name });
      }
      if (this.deptId.includes(element.division)) {
        subClassSet.add({ subclass: element.subclass, sub_Name: element.sub_Name });
      }
      if (this.deptId.includes(element.division)) {
        const deptdata = {
          division: element.division,
          dept_Name: element.dept_Name,
          dept: element.dept,
          class_Name: element.class_Name,
          class: element.class,
          sub_Name: element.sub_Name,
          subclass: element.subclass,
        };
        departmentSet1.add(deptdata);
      }
      if (this.deptId.includes(element.division)) {
        const classdata = { class_Name: element.class_Name, class: element.class };
        classCodeSet1.add(classdata);
      }
      if (this.deptId.includes(element.division)) {
        const subclassdata = { sub_Name: element.sub_Name, subclass: element.subclass };
        subClassSet1.add(subclassdata);
      }
    });

    // const departmentList = Array.from(departmentSet).map((deptName) => ({
    //   text: deptName,
    // }));
    const subClassCodeList = Array.from(subClassSet).map((obj: any) => ({
      key: obj.subclass,
      text: obj.sub_Name,
    }));
    const classCodeList = Array.from(classCodeSet).map((obj: any) => ({
      key: obj.class,
      text: obj.class_Name,
    }));
    const departmentList = Array.from(departmentSet).map((obj: any) => ({
      key: obj.dept,
      text: obj.dept_Name,
    }));
    this.setState({
      departmentListData: departmentList,
      Sub_class_codeListData: subClassCodeList,
      ClassCodeListData: classCodeList,
      departmentListData1: Array.from(departmentSet1),
      Sub_class_codeListData1: Array.from(subClassSet1),
      ClassCodeListData1: Array.from(classCodeSet1),
    });
  }
  DropdownFilter = async () => {
    const Deptlist = new Set();
    const subClassSet = new Set();
    const classCodeSet = new Set();
    const filteredData1 = this.state.deptOption.filter((item: any) => {
      return this.state.selecteddept.includes(item.text);
    });
    const filteredData = this.state.departmentListData1.filter((item: any) => {
      return filteredData1.some((deptItem: any) => deptItem.key === item.division);
    });
    if (this.state.selecteddepartment.length == 0) {
      const departmentList = Array.from(filteredData).map((item: any) => ({
        key: item.dept,
        text: item.dept_Name,
      }));
      // departmentList.forEach((item) => {
      //   Deptlist.add(item.text);
      // });
      //departmentList.sort((a: any, b: any) => a.text.localeCompare(b.text));
      //this.setState({ departmentListData: departmentList });
      departmentList.forEach((item) => {
        Deptlist.add({ key: item.key, text: item.text });
      });
      const uniqueDepartmentList = Array.from(Deptlist).filter(
        (dept: any, index, self) => self.findIndex((d: any) => d.text === dept.text) === index,
      );
      uniqueDepartmentList.sort((a: any, b: any) => a.text.localeCompare(b.text));
      this.setState({ departmentListData: uniqueDepartmentList });
    } else {
      filteredData?.forEach((entry: any) => {
        if (
          this.state.selecteddepartment.includes(entry.dept_Name) &&
          this.state.selectedclass.length === 0
        ) {
          classCodeSet.add({ class: entry.class, class_Name: entry.class_Name });
        }

        if (
          this.state.selecteddepartment.includes(entry.dept_Name) &&
          this.state.selectedclass.includes(entry.class_Name) &&
          this.state.selectedclass.length != 0
        ) {
          subClassSet.add({ subclass: entry.subclass, sub_Name: entry.sub_Name });
        }
      });

      const classCodeList = Array.from(classCodeSet).map((obj: any) => ({
        key: obj.class,
        text: obj.class_Name,
      }));
      // classCodeList.forEach((item) => {
      //   Deptlist.add({ key: item.key, text: item.text})
      // });
      const uniqueClassList = Array.from(classCodeList).filter(
        (classcode: any, index, self) =>
          self.findIndex((d: any) => d.text === classcode.text) === index,
      );
      uniqueClassList.sort((a: any, b: any) => a.text.localeCompare(b.text));
      //this.setState({ departmentListData: uniqueClassList });

      const subClassCodeList = Array.from(subClassSet).map((obj: any) => ({
        key: obj.subclass,
        text: obj.sub_Name,
      }));
      // departmentList.forEach((item) => {
      //   Deptlist.add({ key: item.key, text: item.text})
      // });
      const uniqueSubClassList = Array.from(subClassCodeList).filter(
        (subclass: any, index, self) =>
          self.findIndex((d: any) => d.text === subclass.text) === index,
      );
      uniqueSubClassList.sort((a: any, b: any) => a.text.localeCompare(b.text));
      //this.setState({ departmentListData: uniqueDepartmentList });

      if (classCodeList.length !== 0) {
        this.setState({ ClassCodeListData: uniqueClassList });
      }
      if (subClassCodeList.length !== 0) {
        this.setState({
          Sub_class_codeListData: uniqueSubClassList,
        });
      } else {
        this.setState({ Sub_class_codeListData: [] });
      }
    }
  };
  // perpage = () => {
  //   const recordCont = [25, 50, 100, 150, 200];
  //   return recordCont;
  // };
  // Defaultperpage = () => {
  //   const recordCont = 25;
  //   return recordCont;
  // };

  rowSelection = (e: any) => {
    this.setState({ selectedRowData: e.selectedRows });
  };
  handleMinDateChange = (date: any) => {
    this.setState({ fromDate: date });
  };
  handleToDateChange = (date: any) => {
    this.setState({ toDate: date });
  };
  ClearDates() {
    if (this.commonDateRef.current) {
      this.commonDateRef.current.resetDates();
    }
  }
  expandableRowsComponent = () => {
    if (this.state.expandedRowdata1 !== 0) {
      return <this.UIDExpandableRowComponent row={this.state.rowData} />;
    } else {
      return <></>;
    }
  };
  UIDExpandableRowComponent: React.FC<UIDExpandableRowComponentProps> = (props: { row: any }) => {
    const [datas, setDatas] = useState<UIDExpandableRowComponentState['datas']>(undefined);
    // const [datas1, setDatas1] = useState<UIDExpandableRowComponentState['datas']>(undefined);
    const [loading, setLoading] = useState<UIDExpandableRowComponentState['loading']>(true);
    const [table, setTable] = useState<any>(undefined);

    const apiClient = new ApiManager().CreateApiClient();
    const getIdString = (ids: any) => {
      return ids && ids.length !== 0
        ? ids
          .filter((id: any) => id !== 0)
          .map((id: any) => id.toString())
          .join(',')
        : undefined;
    };
    useEffect(() => {
      const fetchData = async () => {
        try {
          this.setState({ expansion: true });
          const divisionId =
            this.state.expandedRowdata == undefined
              ? this.state.expandedRowdata1
              : this.state.expandedRowdata;
          const dept = getIdString(this.state.selectedDepartmentIds);
          const deptclass = getIdString(this.state.selectedClassIds);
          const subclass = getIdString(this.state.selectedSubClassIds);
          const Fromdate = this.state.fromDate?.toLocaleDateString('en-CA') || undefined;
          const Todate = this.state.toDate?.toLocaleDateString('en-CA') || undefined;
          if (divisionId != null) {
            const dashboardPlanoexpand = await apiClient.getDeptAllCount(
              divisionId,
              dept,
              deptclass,
              subclass,
              Fromdate,
              Todate,
            );
            setDatas(
              dashboardPlanoexpand.result?.map((item: any, index: any) => ({
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
                        <td className={`${expandableTable.uidDivCol} stylec`}>
                          {item.dept_name ?? ''}
                        </td>
                        <td className={expandableTable.countCols}>{item.dept_counts ?? ''}</td>
                        <td className={expandableTable.countCols}>{item.buyer1_Count ?? ''}</td>
                        <td className={expandableTable.countCols}>{item.buyer2_Count ?? ''}</td>
                        {/* <td className={expandableTable.countCols}>{item.fab_Count ?? ''}</td> */}
                        <td className={expandableTable.countCols}>{item.packaging_Count ?? ''}</td>
                        <td className={expandableTable.countCols}>{item.qc_Count ?? ''}</td>
                        <td className={expandableTable.countCols}>{item.itemcode_Ready ?? ''}</td>
                        <td className={expandableTable.countCols}>{item.cam_Ready ?? ''}</td>
                      </tr>
                    </tbody>
                  </table>
                ),
                ...item,
              })) ?? [],
            );
            // setDatas1(
            //   dashboardPlanoexpand.result?.map((item: any, index: any) => ({
            //     index: index + 1,
            //     columnName: (
            //       <table className={expandableTable.table}>
            //         <tbody>
            //           <tr className={expandableTable.row}>
            //             <td className={expandableTable.expandIconCol}></td>
            //             <td className={expandableTable.uidDivCol}>{item.dept_name ?? ''}</td>
            //             <td className={expandableTable.countCols}>{item.dept_counts ?? ''}</td>
            //             <td className={expandableTable.countCols}>{item.buyer1_Count ?? ''}</td>
            //             <td className={expandableTable.countCols}>{item.buyer2_Count ?? ''}</td>
            //             <td className={expandableTable.countCols}>{item.fab_Count ?? ''}</td>
            //             <td className={expandableTable.countCols}>{item.packaging_Count ?? ''}</td>
            //             <td className={expandableTable.countCols}>{item.qc_Count ?? ''}</td>
            //             <td className={expandableTable.countCols}>{item.itemcode_Ready ?? ''}</td>
            //             <td className={expandableTable.countCols}>{item.cam_Ready ?? ''}</td>
            //           </tr>
            //         </tbody>
            //       </table>
            //     ),
            //     ...item,
            //   })) ?? [],
            // );
            setLoading(false);
          }
        } catch (error: any) {
          this.notify.showErrorNotify(error);
        }
      };
      fetchData();
    }, []);

    return (
      <div>
        {this.state.expandedRowdata1 && (
          <div>
            {loading ? (
              <div>Loading...</div>
            ) : datas && datas.length > 0 ? (
              datas.map((item: any, index: any) => (
                <div key={index} style={{ minWidth: '80px' }}>
                  <div className={columndata}>{item.columnName}</div>
                </div>
              ))
            ) : (
              // <div>No data available</div>
              <></>
            )}
          </div>
        )}
      </div>
    );
  };
  handleRowExpand = (row: any, expanded: any) => {
    if (expanded && expanded.divisionCode != 'Total') {
      this.setState({
        expandedRowdata: expanded.divisionCode,
        expandedRowdata1: expanded.divisionCode,
      });
    } else {
      this.setState({ expandedRowdata: null, expandedRowdata1: null });
    }
    if (row) {
      this.setState({ expansionDivRecords: [...this.state.expansionDivRecords, expanded.divisionCode] });
    } else {
      const filteredRecords: any = this.state.expansionDivRecords?.filter((f: any) => f != expanded.divisionCode)
      this.setState({ expansionDivRecords: filteredRecords });
      if (filteredRecords?.length === 0) {
        this.setState({ expansion: false });
      }
    }
  };

  downloadFile = async () => {
    var fromDateStr = this.state.fromDate?.toLocaleDateString('en-CA') || '';
    var toDateStr = this.state.toDate?.toLocaleDateString('en-CA') || '';
    const desiredOrder = this.state.divisionorder.map((item: any) => item.key);
    const sortByDesiredOrder = (ids: any, order: any) => {
      return ids.slice().sort((a: any, b: any) => order.indexOf(a) - order.indexOf(b));
    };
    const sortedDivisionIds = this.state.expansion
      ? this.state.expansionDivRecords
      : sortByDesiredOrder(this.state.selectedDivisionIds, desiredOrder);
    const division = sortedDivisionIds?.join(',');
    const dept_code = this.state.selectedDepartmentIds?.join(',');
    const class_code = [...new Set(this.state.selectedClassIds)]?.join(',');
    const subclass_code = [...new Set(this.state.selectedSubClassIds)]?.join(',');
    this.notify.showSuccessNotify1(this.props.t('DOWNLOAD_STARTED'));
    document.body.classList.add('loading-indicator');

    const response = new GetTemplatesUidCountExcel();
    const dcData: ExportDept[] = [];
    let ids: any = [61, 4, 65];
    if (this.state.selectedDivisionIds?.length > 0) {
      ids = ids?.filter((e: any) => this.state.selectedDivisionIds.includes(e) || this.state.selectedDivisionIds.includes(e?.toString()))
    }
    ids.forEach((dcId: any) => {
      dcData.push(new ExportDept({
        division_code: dcId,
        status: this.state.expansion ? (sortedDivisionIds?.includes(dcId) || sortedDivisionIds?.includes(dcId?.toString())) : false

      }));
    });

    response.data = dcData;
    response.division_code = division || null;
    response.dept_code = dept_code || null;
    response.class_code = class_code || undefined;
    response.subclass_code = subclass_code || undefined;
    response.fromDate = fromDateStr || null;
    response.toDate = toDateStr || null;

    var url_ = `${baseUrl}/api/DashBoard/GetTemplatesUidCountExcel`;

    // if (!(fromDateStr === null || fromDateStr === '')) {
    //   url_ += 'fromDate=' + encodeURIComponent(fromDateStr) + '&';
    // }
    // if (!(toDateStr === null || toDateStr === '')) {
    //   url_ += 'toDate=' + encodeURIComponent(toDateStr) + '&';
    // }
    // if (division && division.length > 0) {
    //   url_ += 'division_code=' + encodeURIComponent(division) + '&';
    // }
    // if (dept_code && dept_code.length > 0) {
    //   url_ += 'dept_code=' + encodeURIComponent(dept_code) + '&';
    // }
    // if (class_code && class_code.length > 0) {
    //   url_ += 'class_code=' + encodeURIComponent(class_code) + '&';
    // }
    // if (subclass_code && subclass_code.length > 0) {
    //   url_ += 'subclass_code=' + encodeURIComponent(subclass_code) + '&';
    // }
    // url_ = url_.replace(/[?&]$/, '');
    axios({
      url: url_,
      method: 'POST',
      responseType: 'blob',
      data: response,
      headers: {
        Token: this.token,
        'Content-Type': 'application/json;',
      },
    })
      .then((response: any) => {
        document.body.classList.remove('loading-indicator');
        this.notify.hideErrorNotify();
        this.notify.showSuccessNotify(this.props.t('DOWNLOAD_COMPLETED'));
        const blob: any = new Blob([response.data], { type: 'application/octet-stream' });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.setAttribute('download', 'UidCountExcel.xlsx');
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
      })
      .catch((error: any) => {
        console.error(error);
      });
  };

  Filterdata = () => { };

  calculatePercentages() {
    const datas = this.state.rowData1;
    if (!datas) return [];

    const totalCounts = {
      houseHold: 0,
      furniture: 0,
      cashAndCarry: 0,
    };

    datas.forEach((data: any) => {
      const uidCount = parseInt(data.uidCount) || 0;
      switch (data.divisionCode) {
        case '61':
          totalCounts.houseHold += uidCount;
          break;
        case '4':
          totalCounts.furniture += uidCount;
          break;
        case '65':
          totalCounts.cashAndCarry += uidCount;
          break;
        default:
          break;
      }
    });

    const total = totalCounts.houseHold + totalCounts.furniture + totalCounts.cashAndCarry;

    return [
      {
        y: parseFloat(((totalCounts.houseHold / total) * 100).toFixed(2)),
        x: 'House Hold',
        color: '#0078D4',
      },
      {
        y: parseFloat(((totalCounts.furniture / total) * 100).toFixed(2)),
        x: 'Furniture',
        color: '#0078D4',
      },
      {
        y: parseFloat(((totalCounts.cashAndCarry / total) * 100).toFixed(2)),
        x: 'Cash And Carry',
        color: '#0078D4',
      },
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
    const {
      deptOption,
      SeasonListData,
      departmentListData,
      ClassCodeListData,
      Sub_class_codeListData,
    } = this.state;
    const selecteddept =
      deptOption && deptOption.length
        ? this.state.selecteddept.length === deptOption.length
        : false;
    const allDeptSelected =
      departmentListData && departmentListData.length
        ? this.state.selecteddepartment.length === departmentListData.length
        : false;
    const allClassSelected =
      ClassCodeListData && ClassCodeListData?.length
        ? this.state.selectedclass.length === ClassCodeListData.length
        : false;
    const allSubClassSelected =
      Sub_class_codeListData && Sub_class_codeListData?.length
        ? this.state.selectedsubclass.length === Sub_class_codeListData.length
        : false;
    var columns: any = [
      {
        key: 'divison',
        name: <div className={columnHeader}>{`${this.props.t('DIVISION')}`}</div>,
        selector: (row: any) => {
          if (row.divisionCode) {
            return <span title={row.divisionName}>{row.divisionName}</span>;
          }
        },
        isVisible: true,
        minWidth: '150px',
      },
      {
        key: 'totaluidgenerated',
        name: <div className={columnHeader}>{`${this.props.t('COL_TOTAL_UID_GENERATED')}`}</div>,
        selector: (row: any) => <span title={row.uidCount}>{row.uidCount}</span>,
        isVisible: true,
        minWidth: '100px',
      },
      {
        key: 'buyerTemp1',
        name: <div className={columnHeader}>{`${this.props.t('CAL_BUYER_TEMP1')}`}</div>,
        selector: (row: any) => <span title={row.buyerTemplate1}>{row.buyerTemplate1}</span>,
        minwidth: '100px',
        isVisible: true,
      },
      {
        key: 'buyerTemp2',
        name: <div className={columnHeader}>{`${this.props.t('CAL_BUYER_TEMP2')}`}</div>,
        selector: (row: any) => <span title={row.buyerTemplate2}>{row.buyerTemplate2}</span>,
        isVisible: true,
        minWidth: '100px',
      },
      // {
      //   key: 'fabTemp',
      //   name: <div className={columnHeader}>{`${this.props.t('CAL_FAB_TEMP')}`}</div>,
      //   selector: (row: any) => <span title={row.fab}>{row.fab}</span>,
      //   isVisible: true,
      //   minWidth: '100px',
      // },
      {
        key: 'designerTemp',
        name: <div className={columnHeader}>{this.props.t('CAL_DESIGNER_TEMP')}</div>,
        position: 'fixed',
        selector: (row: any) => <span title={row.packagingDesigner}>{row.packagingDesigner}</span>,
        minWidth: '100px',
      },
      {
        key: 'qcTemp',
        name: <div className={columnHeader}>{`${this.props.t('COL_QC_TEMP')}`}</div>,
        selector: (row: any) => <span title={row.qcTemplate}>{row.qcTemplate}</span>,
        minWidth: '100px',
        isVisible: true,
      },
      {
        key: 'itemCodeReady',
        name: <div className={columnHeader}>{`${this.props.t('CAL_ITEMCODE_READY')}`}</div>,
        selector: (row: any) => <span title={row.itemcode_Ready}>{row.itemcode_Ready}</span>,
        isVisible: true,
        minWidth: '100px',
      },
      {
        key: 'camReady',
        name: <div className={columnHeader}>{`${this.props.t('CAL_CAM_READY')}`}</div>,
        selector: (row: any) => <span title={row.cam_Ready}>{row.cam_Ready}</span>,
        isVisible: true,
        minWidth: '100px',
      },
    ];

    const handleColumnReorder = (newColumns: any) => {
      var _column: any = [...columns];
      if (newColumns == this.state.currentColumns) {
        this.setState({ currentColumns: _column });
      } else {
        this.setState({ currentColumns: newColumns });
      }
    };

    const customPieChartStyles: IStyleFunctionOrObject<IPieChartStyleProps, IPieChartStyles> = {
      chartTitle: {
        marginRight: '66px',
        fontWeight: 'bold',
      },
    };

    return (
      <div className="Uidrecordslayout">
        <div className="Uidrecords" />
        <Grid lg={12} item container spacing={1.5} direction={'row'}>
          <Grid item lg={11.4} xs={1}></Grid>
          <div className="UIDdownload">
            <Grid item lg={0.5} xs={1}>
              <Icon
                iconName="Download"
                className="iconStyle"
                title={this.props.t('BTN_DOWNLOAD')}
                onClick={() => {
                  if (this.state.DashboardUid.length !== 0) {
                    this.downloadFile();
                  }
                }}
              />
            </Grid>
          </div>
        </Grid>
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
              key={this.state.refreshKey4}
              onChange={(e: any, selectedOption: any) => {
                this.setState({
                  selecteddepartment: [],
                  selectedDepartmentIds: [],
                  ClassCodeListData: [],
                  Sub_class_codeListData: [],
                  refreshKey: this.state.refreshKey + 1,
                  refreshKey1: this.state.refreshKey1 + 1,
                  refreshKey2: this.state.refreshKey2 + 1,
                  selectedsubclass: [],
                  selectedSubClassIds: [],
                  selectedclass: [],
                  selectedClassIds: [],
                });
                if (selectedOption && selectedOption.key === 'select-toggle') {
                  const allSelected =
                    (this.state.selecteddept || []).length === (deptOption || []).length;
                  const newSelectedSeasons = allSelected
                    ? []
                    : deptOption
                      .map((option: any) => (option.text === 'Select All' ? null : option.text))
                      .filter(Boolean);
                  const newSelectedSeasons1 = allSelected
                    ? []
                    : deptOption
                      .map((option: any) => (option.text === 'Select All' ? null : option.key))
                      .filter(Boolean);
                  this.setState(
                    {
                      selecteddept: newSelectedSeasons,
                      selectedDivisionIds: newSelectedSeasons1,
                      searchKey: '',
                      divisionSelected: '',
                    },
                    () => {
                      this.setState({
                        ClassCodeListData: [],
                        Sub_class_codeListData: [],
                        refreshKey: this.state.refreshKey + 1,
                        refreshKey1: this.state.refreshKey1 + 1,
                        refreshKey2: this.state.refreshKey2 + 1,
                        selectedsubclass: [],
                        selectedSubClassIds: [],
                        selectedclass: [],
                        selectedClassIds: [],
                      });
                      this.DropdownFilter();
                    },
                  );
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
                    updatedSelectedSeasons.splice(index, 1);
                    const index1 = updatedSelectedSeasons1.indexOf(selectedOption.key);
                    updatedSelectedSeasons1.splice(index1, 1);
                  } else {
                    updatedSelectedSeasons.push(selectedOption.text);
                    updatedSelectedSeasons1.push(selectedOption.key);
                  }
                  this.setState(
                    {
                      selecteddept: updatedSelectedSeasons,
                      selectedDivisionIds: updatedSelectedSeasons1,
                      divisionSelected: '',
                      searchKey: '',
                    },
                    () => {
                      this.setState({
                        ClassCodeListData: [],
                        Sub_class_codeListData: [],
                        refreshKey: this.state.refreshKey + 1,
                        refreshKey1: this.state.refreshKey1 + 1,
                        refreshKey2: this.state.refreshKey2 + 1,
                        selectedsubclass: [],
                        selectedSubClassIds: [],
                        selectedclass: [],
                        selectedClassIds: [],
                      });
                      this.DropdownFilter();
                    },
                  );
                }
              }}
              selectedKey={this.state.selecteddept || []}
            />
          </Grid>
          <Grid item lg={1.9} xs={6}>
            <ComboBox
              multiSelect
              placeholder={`${t('UID_DEPT_DROPDOWN')}`}
              disabled={this.state.selecteddept.length == 0}
              options={[
                { key: 'select-toggle', text: 'Select All', selected: allDeptSelected },
                ...(this.state.departmentListData || []).map((option: any) => ({
                  key: option.key,
                  text: option.text,
                  selected: Array.isArray(this.state.selecteddepartment)
                    ? this.state.selecteddepartment.includes(option.text)
                    : false,
                })),
              ]}
              // options={[
              //   { key: 'select-toggle', text: 'Select All', selected: selecteddept },
              //   ...(deptOption || []).map((option: any) => ({
              //     key: option.key,
              //     text: option.text,
              //     selected: Array.isArray(this.state.selecteddept)
              //       ? this.state.selecteddept.includes(option.text)
              //       : false,
              //   })),
              // ]}
              styles={PIMDropdownStyles1}
              key={this.state.refreshKey}
              onChange={(e: any, selectedOption: any) => {
                this.setState({
                  ClassCodeListData: [],
                  Sub_class_codeListData: [],
                  refreshKey1: this.state.refreshKey1 + 1,
                  refreshKey2: this.state.refreshKey2 + 1,
                  selectedsubclass: [],
                  selectedSubClassIds: [],
                  selectedclass: [],
                  selectedClassIds: [],
                });
                if (selectedOption && selectedOption.key === 'select-toggle') {
                  const allSelected1 =
                    (this.state.selecteddepartment || []).length ===
                    (this.state.departmentListData || []).length;
                  const newSelectedSeasons = allSelected1
                    ? []
                    : (this.state.departmentListData || [])
                      .map((option: any) => (option.text === 'Select All' ? null : option.text))
                      .filter(Boolean);
                  const newSelectedSeasons1 = allSelected1
                    ? []
                    : (this.state.departmentListData || [])
                      .map((option: any) => (option.text === 'Select All' ? null : option.key))
                      .filter(Boolean);

                  this.setState(
                    {
                      selecteddepartment: newSelectedSeasons,
                      selectedDepartmentIds: newSelectedSeasons1,
                    },
                    () => {
                      this.setState({
                        ClassCodeListData: [],
                        Sub_class_codeListData: [],
                        refreshKey1: this.state.refreshKey1 + 1,
                        refreshKey2: this.state.refreshKey2 + 1,
                        selectedsubclass: [],
                        selectedSubClassIds: [],
                        selectedclass: [],
                        selectedClassIds: [],
                      });
                      // this.columnFilter();
                      this.DropdownFilter();
                    },
                  );
                } else {
                  this.setState({
                    ClassCodeListData: [],
                    Sub_class_codeListData: [],
                    refreshKey1: this.state.refreshKey1 + 1,
                    refreshKey2: this.state.refreshKey2 + 1,
                    selectedsubclass: [],
                    selectedSubClassIds: [],
                    selectedclass: [],
                    selectedClassIds: [],
                  });
                  const updatedSelectedDept = Array.isArray(this.state.selecteddepartment)
                    ? [...this.state.selecteddepartment]
                    : [];
                  const updatedSelectedDept1 = Array.isArray(this.state.selectedDepartmentIds)
                    ? [...this.state.selectedDepartmentIds]
                    : [];
                  const isSelected1 = Array.isArray(this.state.selecteddepartment)
                    ? this.state.selecteddepartment.includes(selectedOption.text)
                    : false;
                  if (isSelected1) {
                    const index = updatedSelectedDept.indexOf(selectedOption.text);
                    updatedSelectedDept.splice(index, 1);
                    const index1 = updatedSelectedDept1.indexOf(selectedOption.key);
                    updatedSelectedDept1.splice(index1, 1);
                  } else {
                    updatedSelectedDept.push(selectedOption.text);
                    updatedSelectedDept1.push(selectedOption.key);
                  }
                  this.setState(
                    {
                      selecteddepartment: updatedSelectedDept,
                      selectedDepartmentIds: updatedSelectedDept1,
                    },
                    () => {
                      this.selectedClassCode1 = [];
                      this.selectedSubClassCode1 = [];
                      this.setState({
                        ClassCodeListData: [],
                        Sub_class_codeListData: [],
                        refreshKey2: this.state.refreshKey2 + 1,
                      });
                      // this.columnFilter();
                      this.DropdownFilter();
                    },
                  );
                }
              }}
              selectedKey={this.state.selecteddepartment || []}
            />
          </Grid>
          <Grid item lg={1.9} xs={6}>
            <ComboBox
              multiSelect
              placeholder={`${t('UID_CLASSNAME_DROPDOWN')}`}
              disabled={this.state.selecteddepartment.length == 0}
              options={[
                { key: 'select-toggle', text: 'Select All', selected: allClassSelected },
                ...(this.state.ClassCodeListData || []).map((option: any) => ({
                  key: option.key,
                  text: option.text,
                  selected: Array.isArray(this.state.selectedclass)
                    ? this.state.selectedclass.includes(option.text)
                    : false,
                })),
              ]}
              styles={PIMDropdownStyles1}
              key={this.state.refreshKey1}
              onChange={(e: any, selectedOption: any) => {
                this.setState({ refreshKey2: this.state.refreshKey2 + 1 });
                if (selectedOption && selectedOption.key === 'select-toggle') {
                  const allSelected1 =
                    (this.state.selectedclass || []).length ===
                    (this.state.ClassCodeListData || []).length;
                  const newSelectedSeasons = allSelected1
                    ? []
                    : (this.state.ClassCodeListData || [])
                      .map((option: any) => (option.text === 'Select All' ? null : option.text))
                      .filter(Boolean);
                  const newSelectedSeasons1 = allSelected1
                    ? []
                    : (this.state.ClassCodeListData || [])
                      .map((option: any) => (option.text === 'Select All' ? null : option.key))
                      .filter(Boolean);

                  this.setState(
                    { selectedclass: newSelectedSeasons, selectedClassIds: newSelectedSeasons1 },
                    () => {
                      this.setState({ refreshKey2: this.state.refreshKey2 + 1 });
                      // this.columnFilter();
                      this.DropdownFilter();
                    },
                  );
                } else {
                  const updatedSelectedDept = Array.isArray(this.state.selectedclass)
                    ? [...this.state.selectedclass]
                    : [];
                  const updatedSelectedDept1 = Array.isArray(this.state.selectedClassIds)
                    ? [...this.state.selectedClassIds]
                    : [];
                  const isSelected1 = Array.isArray(this.state.selectedclass)
                    ? this.state.selectedclass.includes(selectedOption.text)
                    : false;
                  if (isSelected1) {
                    const index = updatedSelectedDept.indexOf(selectedOption.text);
                    updatedSelectedDept.splice(index, 1);
                    const index1 = updatedSelectedDept1.indexOf(selectedOption.key);
                    updatedSelectedDept1.splice(index1, 1);
                  } else {
                    updatedSelectedDept.push(selectedOption.text);
                    updatedSelectedDept1.push(selectedOption.key);
                  }
                  this.setState(
                    { selectedclass: updatedSelectedDept, selectedClassIds: updatedSelectedDept1 },
                    () => {
                      this.setState({ refreshKey2: this.state.refreshKey2 + 1 });
                      // this.columnFilter();
                      this.DropdownFilter();
                    },
                  );
                }
              }}
              selectedKey={this.state.selectedclass || []}
            />
          </Grid>
          <Grid item lg={1.9} xs={6}>
            <ComboBox
              multiSelect
              placeholder={`${t('UID_SUBCLASSNAME_DROPDOWN')}`}
              disabled={this.state.selectedclass.length == 0}
              options={[
                { key: 'select-toggle', text: 'Select All', selected: allSubClassSelected },
                ...(this.state.Sub_class_codeListData || []).map((option: any) => ({
                  key: option.key,
                  text: option.text,
                  selected: Array.isArray(this.state.selectedsubclass)
                    ? this.state.selectedsubclass.includes(option.text)
                    : false,
                })),
              ]}
              styles={PIMDropdownStyles1}
              onChange={(e: any, selectedOption: any) => {
                if (selectedOption && selectedOption.key === 'select-toggle') {
                  const allSelected1 =
                    (this.state.selectedsubclass || []).length ===
                    (this.state.Sub_class_codeListData || []).length;
                  const newSelectedSeasons = allSelected1
                    ? []
                    : (this.state.Sub_class_codeListData || [])
                      .map((option: any) => (option.text === 'Select All' ? null : option.text))
                      .filter(Boolean);
                  const newSelectedSeasons1 = allSelected1
                    ? []
                    : (this.state.Sub_class_codeListData || [])
                      .map((option: any) => (option.text === 'Select All' ? null : option.key))
                      .filter(Boolean);

                  this.setState(
                    {
                      selectedsubclass: newSelectedSeasons,
                      selectedSubClassIds: newSelectedSeasons1,
                    },
                    () => {
                      // this.columnFilter();
                    },
                  );
                } else {
                  const updatedSelectedDept = Array.isArray(this.state.selectedsubclass)
                    ? [...this.state.selectedsubclass]
                    : [];
                  const updatedSelectedDept1 = Array.isArray(this.state.selectedSubClassIds)
                    ? [...this.state.selectedSubClassIds]
                    : [];
                  const isSelected1 = Array.isArray(this.state.selectedsubclass)
                    ? this.state.selectedsubclass.includes(selectedOption.text)
                    : false;
                  if (isSelected1) {
                    const index = updatedSelectedDept.indexOf(selectedOption.text);
                    updatedSelectedDept.splice(index, 1);
                    const index1 = updatedSelectedDept1.indexOf(selectedOption.key);
                    updatedSelectedDept1.splice(index1, 1);
                  } else {
                    updatedSelectedDept.push(selectedOption.text);
                    updatedSelectedDept1.push(selectedOption.key);
                  }
                  this.setState(
                    {
                      selectedsubclass: updatedSelectedDept,
                      selectedSubClassIds: updatedSelectedDept1,
                    },
                    () => {
                      // this.columnFilter();
                    },
                  );
                }
              }}
              key={this.state.refreshKey2}
              selectedKey={this.state.selectedsubclass || []}
            />
          </Grid>
          <Grid item lg={2.15} xs={6}>
            <CommonDate
              onMinDateChange={(e: any) => this.handleMinDateChange(e)}
              onToDateChange={(e: any) => this.handleToDateChange(e)}
              ref={this.commonDateRef}
            // label={`${t('RECEIVED_DATE')}`}
            />
            {/* placeholder={t('RECEIVED_DATE_PALCEHOLDER')} */}
          </Grid>
          <Grid item lg={0.733} xs={1} />
          <Grid item lg={0.5} xs={2}>
            <Icon
              iconName="Search"
              title={this.props.t('BTN_SEARCH')}
              className="iconStyle"
              onClick={() => {
                if (
                  (this.state.selectedSubClassIds && this.state.selectedSubClassIds.length > 0) ||
                  (this.state.selectedClassIds && this.state.selectedClassIds.length > 0) ||
                  (this.state.selectedDepartmentIds &&
                    this.state.selectedDepartmentIds.length > 0) ||
                  (this.state.selectedDivisionIds && this.state.selectedDivisionIds.length > 0) ||
                  this.state.fromDate ||
                  this.state.toDate
                ) {
                  this.Adddivision();
                  this.setState(
                    {
                      filteredRecord: [],
                      isRecord: true,
                      // expandedRowdata: null,
                      expandflag: true,
                    },
                    () => {
                      this.onSearchClick(this.state.selectedDivisionIds);
                    },
                  );
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
                  (this.state.selectedSubClassIds && this.state.selectedSubClassIds.length > 0) ||
                  (this.state.selectedClassIds && this.state.selectedClassIds.length > 0) ||
                  (this.state.selectedDepartmentIds &&
                    this.state.selectedDepartmentIds.length > 0) ||
                  (this.state.selectedDivisionIds && this.state.selectedDivisionIds.length > 0) ||
                  this.state.fromDate ||
                  this.state.toDate
                ) {
                  this.setState({
                    expandflag: false,
                    filteredRecord: [],
                    fromDate: null,
                    toDate: null,
                    selecteddept: [],
                    selectedDivisionIds: [],
                    selecteddepartment: [],
                    selectedDepartmentIds: [],
                    ClassCodeListData: [],
                    Sub_class_codeListData: [],
                    refreshKey: this.state.refreshKey + 1,
                    refreshKey1: this.state.refreshKey1 + 1,
                    refreshKey2: this.state.refreshKey2 + 1,
                    refreshKey4: this.state.refreshKey4 + 1,
                    selectedsubclass: [],
                    selectedSubClassIds: [],
                    selectedclass: [],
                    selectedClassIds: [],
                    expandedRowdata: null,
                  });
                  this.refresh();
                  this.ClearDates();
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
                this.setState({
                  expandflag: false,
                  filteredRecord: [],
                  fromDate: null,
                  toDate: null,
                  selecteddept: [],
                  selectedDivisionIds: [],
                  selecteddepartment: [],
                  selectedDepartmentIds: [],
                  ClassCodeListData: [],
                  Sub_class_codeListData: [],
                  refreshKey: this.state.refreshKey + 1,
                  refreshKey1: this.state.refreshKey1 + 1,
                  refreshKey2: this.state.refreshKey2 + 1,
                  refreshKey4: this.state.refreshKey4 + 1,
                  selectedsubclass: [],
                  selectedSubClassIds: [],
                  selectedclass: [],
                  selectedClassIds: [],
                });
                this.refresh();
                this.ClearDates();
              }}
            />
          </Grid>
        </Grid>
        <div className="tablegraph1">
          <div className="UIDDatatable">
            {this.state.expandflag === true ? (
              <DataTable
                className="dashboard"
                columns={columns}
                data={this.state.filteredRecord}
                // pagination={true}
                expandableRows
                expandableRowsComponent={this.expandableRowsComponent}
                expandableRowDisabled={(row) => row.division_Id == 0}
                selectableRowsHighlight={true}
                clearSelectedRows={this.state.noSelection}
                selectableRowsVisibleOnly={this.state.selectedRowData}
                expandableRowExpanded={(row) => row.divisionCode === this.state.expandedRowdata1}
                // expandableIcon={{
                //   expanded: shouldDisplayIcon ? (
                //     null
                //   ) : (
                //     <Icon iconName="Refresh" />
                //   ),
                //   collapsed: shouldDisplayIcon ? null : <Icon iconName="Refresh" />,
                // }}
                onSelectedRowsChange={this.rowSelection}
                onColumnOrderChange={handleColumnReorder}
                onRowExpandToggled={this.handleRowExpand}
                highlightOnHover
                fixedHeader
                striped
                fixedHeaderScrollHeight="61.8vh"
                // paginationComponentOptions={{ rowsPerPageText: `${this.props.t('ROWS_PER_PAGE')}` }}
                // paginationPerPage={this.Defaultperpage()}
                // paginationRowsPerPageOptions={this.perpage()}
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
            ) : (
              <DataTable
                className="dashboard"
                columns={columns}
                data={this.state.filteredRecord}
                // pagination={true}
                expandableRows
                expandableRowsComponent={this.expandableRowsComponent}
                expandableRowDisabled={(row) => row.division_Id == 0}
                selectableRowsHighlight={true}
                clearSelectedRows={this.state.noSelection}
                selectableRowsVisibleOnly={this.state.selectedRowData}
                // expandableIcon={{
                //   expanded: shouldDisplayIcon ? (
                //     null
                //   ) : (
                //     <Icon iconName="Refresh" />
                //   ),
                //   collapsed: shouldDisplayIcon ? null : <Icon iconName="Refresh" />,
                // }}
                onSelectedRowsChange={this.rowSelection}
                onColumnOrderChange={handleColumnReorder}
                onRowExpandToggled={this.handleRowExpand}
                highlightOnHover
                fixedHeader
                striped
                fixedHeaderScrollHeight="69.1vh"
                // paginationComponentOptions={{ rowsPerPageText: `${this.props.t('ROWS_PER_PAGE')}` }}
                // paginationPerPage={this.Defaultperpage()}
                // paginationRowsPerPageOptions={this.perpage()}
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
            )}
          </div>
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
            <div className="legends">
              {legends.length > 0 && <Legends legends={legends} className="legend" />}
            </div>
          </div>
        </div>
      </div>
    );
  }
}

const ComponentTranslated = withTranslation()(DashboardUid);
function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}
export default App;
