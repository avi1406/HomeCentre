import {
  ComboBox,
  createTheme,
  DefaultButton,
  Icon,
  IconButton,
  IDropdownOption,
  Modal,
  SearchBox,
  Stack,
  TextField,
} from '@fluentui/react';
import React, { Suspense } from 'react';
import DataTable from 'react-data-table-component';
import { Text } from '@fluentui/react/lib/Text';
import { Grid } from '@mui/material';
import {
  PIMButtons,
  PIMsearchBoxStyles,
  columnHeader,
  PIMcontentStyles,
  PIMfooterButtonConfirm,
  PIMfooterButtonCancel,
  customTableStyles1,
  PIMHearderText,
} from '@/pages/PimStyles';
import ApiManager from '@/services/ApiManager';
import AuthManagerService from '@/services/AuthManagerService';
import { withTranslation } from 'react-i18next';
import jwt_decode from 'jwt-decode';
import NotificationManager from '@/services/NotificationManager';
import { Link } from 'react-router-dom';
import Util from '@/services/Util';
import { MarketplaceTemplateAttribute, MarketPlaceUpload } from '@/services/ApiClient';
import { cancelIcon, iconButtonStyles } from '@/common/properties';
import { baseUrl } from '@/services/Constants';
import { downloadFileMarketPlace } from '@/pages/commonFunctions';
import UploadAttribute from '../../Configuration/Market Place/UploadAttributeXl';
import { BlockBlobClient } from '@azure/storage-blob';
import axios from 'axios';
import { styled } from 'office-ui-fabric-react';

class OutputTemplateAttribute extends React.Component<any, any> {
  authManager = new AuthManagerService();
  notify = new NotificationManager();
  apiClient = new ApiManager().CreateApiClient();
  token = sessionStorage.getItem('token');
  decode: any = jwt_decode(this.token || '');
  divisionId: any = this.decode?.DepartmentId.split(',');
  roleId: any = this.decode?.RoleId.split(',');
  roleLength = this.decode?.RoleId.split(',').length;
  util = new Util();
  options: IDropdownOption[] = [
    { key: 0, text: 'Yes' },
    { key: 1, text: 'No' },
  ];
  tempAttribute: any = [];
  constructor(props: any) {
    super(props);
    this.state = {
      searchData: [],
      selectionCount: 0,
      selectedRowData: [],
      noSelection: false,
      isRecord: false,
      searchValue: '',
      isActive: true,
      Title: '',
      marketPlaceTemplateId: 0,
      userDivisionId: null,
      confirmModel: false,
      isReset: true,
      selectedDataType: null,
      marketTemplateAttributeData: [],
      masterAttributeOptions: [],
      MarketPlace: [],
      ProductDepartment: [],
      MarketplaceAttribute: [],
      addAttribute: false,
      marketplaceAttributeOption: [],
      updateData: [],
      validatedData: [],
      selectedMarketAttri: 0,
      dataTypeError: false,
      btnSave: true,
      dataType: [],
      departmentOptionname: [],
      ProgressVisible: false,
      rowIDs: [],
      AttributeValue: {},
      editedAttributes: {},
      editedColumnOrder: {},
      DefaulAttributeValue: {},
      savingValue: false,
      CurrentRowID: '',
      resetsave: [],
      UpdateMD: [],
      inputfield: {},
      inputfield1: {},
    };
  }

  async componentDidMount() {
    this.setState({ isRecord: true });
    this.FilterFundept();
    var masterAttributesList: any = [];
    var marketPlaceList: any = [];
    var productDepartmentList: any = [];
    var divisionID: any;
    this.FilterFun();
    const Ids: any = this.util.getUrlParameterId();
    const id: any = Ids.split(',');
    const viewDataId: any = parseInt(id[0]);
    const Divisionid: any = parseInt(id[1]);
    const responses = await this.apiClient.getMarketplaceTemplateAttributesDiv(viewDataId);
    const Id = responses?.result?.map((Id: any) => {
      return Id.divisionId;
    })[0];
    for (var i = 0; i < this.divisionId.length; i++) {
      if (this.divisionId[i] != 0) {
        this.setState({ userDivisionId: this.divisionId[i] });
        divisionID = this.divisionId[i];
        break;
      }
    }
    const appConfiguration: any = await this.apiClient.getAppConfig();

    const dataType = JSON.parse(
      appConfiguration?.result?.find((e: any) => e.name == 'DataType')?.jsonValue,
    );

    const masterAttributes = (await this.apiClient.listAttributes(Id, true))?.result;
    const marketPlace = await this.apiClient.searchMarketplaces('', 0, 0, true);
    const productDepartment = (await this.apiClient.getDepartments()).result;
    masterAttributes?.forEach((element: any) => {
      masterAttributesList.push({
        key: element.id,
        text: element.name,
        dataType: element.dataType,
      });
    });
    marketPlace.result?.records?.forEach((element: any) => {
      marketPlaceList.push({ key: element.id, text: element.name });
    });
    productDepartment?.forEach((element: any) => {
      productDepartmentList.push({ key: element.id, text: element.name });
    });
    this.setState({
      divisionid: Divisionid,
      masterAttributeOptions: masterAttributesList,
      MarketPlace: marketPlaceList,
      ProductDepartment: productDepartmentList,
      dataType: dataType,
      templateid: viewDataId,
    });
    this.refresh();
  }
  async refresh() {
    this.setState({ inputfield: {}, inputfield1: {} });
    const Ids: any = this.util.getUrlParameterId();
    const id: any = Ids.split(',');
    const viewDataId: any = parseInt(id[0]);
    const templateName: any = decodeURI(id[2]);
    var marketplaceAttributeList: any = [];
    // const responses1 = await this.apiClient.searchMarketplaceTemplateAttributes(
    //   '',
    //   0,
    //   0,
    //   viewDataId,
    // );
    const responses = await this.apiClient.getMarketplaceTemplateAttributes('', 0, 0, viewDataId);
    this.setState({
      marketTemplateAttributeData: [],
    });
    this.setState({
      marketTemplateAttributeData: responses.result,
      updateData: responses.result,
      MarketplaceAttribute: marketplaceAttributeList,
      marketPlaceTemplateId: viewDataId,
      noSelection: !this.state.noSelection,
      selectedRowData: [],
      selectionCount: 0,
      isRecord: false,
      confirmModel: false,
      addAttribute: false,
      dataTypeError: false,
      btnSave: true,
    });
    this.setState({
      marketTemplateAttributeData: [],
    });
    this.setState({
      marketTemplateAttributeData: (responses.result as any)?.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      })),
      searchData: responses.result?.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      })),
      updateData: (responses.result as any)?.map((item: any, index: any) => ({
        index: index + 1,
        ...item,
      })),
      searchValue: '',
      Title: templateName,
      editedAttributes: { ...this.state.editedAttributes, [this.state.CurrentRowID]: undefined },
      editedColumnOrder: { ...this.state.editedColumnOrder, [this.state.CurrentRowID]: undefined },
    });
  }

  handleAttributesEdit = (rowId: any, value: any) => {
    this.setState({
      editedAttributes: { ...this.state.editedAttributes, [rowId]: value },
      CurrentRowID: rowId,
    });
    this.UpdateAttributeMapping(this.state.updateData, rowId, 'marketplaceAttribute', value);
  };

  handleAttributesOrderEdit = (rowId: any, value: any) => {
    this.setState({
      editedColumnOrder: { ...this.state.editedColumnOrder, [rowId]: value },
      CurrentRowID: rowId,
    });

    this.UpdateAttributeMapping(this.state.updateData, rowId, 'columnOrder', value);
  };

  renderAttributesCell = (row: any) => {
    const { editedAttributes } = this.state;
    return (
      <>
        {editedAttributes[row.id] !== undefined ? (
          <TextField
            value={editedAttributes[row.id]}
            onChange={(e: any) => {
              this.handleAttributesEdit(row.id, e.target.value),
                e.target.value === row.marketplaceAttribute
                  ? this.setState({
                      btnSave: true,
                    })
                  : this.setState({
                      btnSave: false,
                    });
            }}
            // onBlur={(e: any) => {
            //   if (this.state.savingValue === false) {
            //     this.setState({
            //       editedAttributes: { ...this.state.editedAttributes, [row.id]: undefined },
            //       btnSave: true,
            //     });
            //   }
            // }}
          />
        ) : (
          <span onClick={() => this.handleAttributesEdit(row.id, row.marketplaceAttribute)}>
            {row.marketplaceAttribute}
          </span>
        )}
      </>
    );
  };

  renderAttributeOrderCell = (row: any) => {
    const { editedColumnOrder } = this.state;
    const masterAttributeCount = this.state.masterAttributeOptions
      ? this.state.masterAttributeOptions.length
      : 0;
    return (
      <>
        {editedColumnOrder[row.id] !== undefined ? (
          <TextField
            value={editedColumnOrder[row.id]}
            onChange={(e: any) => {
              this.handleAttributesOrderEdit(row.id, e.target.value),
                e.target.value === row.columnOrder
                  ? this.setState({
                      btnSave: true,
                    })
                  : this.setState({
                      btnSave: false,
                    });
            }}
            onKeyPress={(e: any) => {
              if (!/^\d*$/.test(e.key)) {
                e.preventDefault();
                this.showErrorNotify1();
              } else if (
                parseInt(e.target.value + e.key) > masterAttributeCount ||
                parseInt(e.target.value + e.key) < 1
              ) {
                e.preventDefault();
                this.notify.showErrorNotify(
                  this.props.t('MARKETPLACE_ORDER_WITHIN_RANGE') + `${masterAttributeCount}`,
                );
              }
            }}
            onPaste={(e: any) => {
              const pastedData = e.clipboardData.getData('Text');
              if (!/^\d*$/.test(pastedData)) {
                e.preventDefault();
                this.showErrorNotify1();
              } else if (parseInt(pastedData) > masterAttributeCount || parseInt(pastedData) < 1) {
                e.preventDefault();
                this.notify.showErrorNotify(
                  this.props.t('MARKETPLACE_ORDER_WITHIN_RANGE') + `${masterAttributeCount}`,
                );
              }
            }}
            onInput={(e: any) => {
              const value = e.target.value;
              if (
                !/^\d*$/.test(value) ||
                parseInt(value) > masterAttributeCount ||
                parseInt(value) < 1
              ) {
                e.preventDefault();
                this.showErrorNotify1();
                this.notify.showErrorNotify(
                  this.props.t('MARKETPLACE_ORDER_WITHIN_RANGE') + `${masterAttributeCount}`,
                );
                e.target.value = value.replace(/[^\d]/g, '');
              }
            }}
          />
        ) : (
          <span onClick={() => this.handleAttributesOrderEdit(row.id, row.columnOrder)}>
            {row.columnOrder}
          </span>
        )}
      </>
    );
  };

  arraysEqual(arr1: any, arr2: any) {
    if (arr1.length !== arr2.length) {
      return false;
    }
    const sortedArr1 = arr1.slice().sort();
    const sortedArr2 = arr2.slice().sort();
    for (let i = 0; i < sortedArr1.length; i++) {
      if (sortedArr1[i] !== sortedArr2[i]) {
        return false;
      }
    }
    return true;
  }
  arraysEqual1(arr1: any, arr2: any) {
    if (!Array.isArray(arr1) || !Array.isArray(arr2)) {
      return false;
    }
    if (arr1.length !== arr2.length) {
      return false;
    }
    const sortedArr1 = arr1.slice().sort();
    const sortedArr2 = arr2.slice().sort();
    for (let i = 0; i < sortedArr1.length; i++) {
      if (sortedArr1[i] !== sortedArr2[i]) {
        return false;
      }
    }
    return true;
  }
  handleDuplicateAttributeOrder = () => {
    const updatedData = this.state.updateData;
    if (!updatedData || updatedData.length === 0) {
      return false;
    }
    const uniqueDatas = new Set();
    for (const data of updatedData) {
      if (data.columnOrder === null || data.columnOrder === '') {
        continue;
      }
      if (uniqueDatas.has(data.columnOrder)) {
        return true;
      }
      uniqueDatas.add(data.columnOrder);
    }
    return false;
  };
  handleMarketplaceAttributeNullCheck = () => {
    const updatedData = this.state.updateData;
    if (!updatedData || updatedData.length === 0) {
      return false;
    }
    for (const data of updatedData) {
      if (
        !(data.columnOrder === null || data.columnOrder === '') &&
        (data.marketplaceAttribute === '' || data.marketplaceAttribute === null)
      ) {
        return true;
      }
    }
    return false;
  };

  async addAttributesTemp() {
    const duplicateOrder = this.handleDuplicateAttributeOrder();
    const marketplaceAttributeNullCheck = this.handleMarketplaceAttributeNullCheck();
    if (!duplicateOrder && !marketplaceAttributeNullCheck) {
      var value: MarketplaceTemplateAttribute[] = [];
      if (this.state.resetsave.length !== 0) {
        this.removeTempalteAttri();
      } else {
        // this.state.updateData;
        var data = true;
        this.state.updateData?.forEach(async (element: any) => {
          if (element?.marketplaceAttribute !== null) {
            if (element?.marketplaceAttribute.trim() === '') {
              this.notify.showErrorNotify(
                this.props.t('PLEASE_FILL_MARCKETPLACEATTRIBUTE_AND_DATAtYPE'),
              );
              data = false;
              this.refresh();
              return;
            }
          }
          // if (
          //   (element?.datatype !== null && element?.marketplaceAttribute === null) ||
          //   (element?.datatype === null && element?.marketplaceAttribute !== null)
          // ) {
          //   this.notify.showErrorNotify(
          //     this.props.t('PLEASE_FILL_MARCKETPLACEATTRIBUTE_AND_DATAtYPE'),
          //   );
          //   data = false;
          //   this.refresh();
          //   return;
          // }
        });
        if (data) {
          this.state.updateData?.forEach(async (element: any) => {
            if (
              element.marketplaceAttribute !== null &&
              element.marketplaceAttribute !== undefined
            ) {
              element.marketplaceAttribute = element.marketplaceAttribute
                .replace(/\s+/g, ' ')
                .trim();
              value.push(
                new MarketplaceTemplateAttribute({
                  isActive: true,
                  marketplaceTemplateId: element?.marketplaceTemplateId,
                  productAttributeId: element?.productAttributeId,
                  marketplaceAttribute: element?.marketplaceAttribute,
                  columnOrder: element?.columnOrder,
                  datatype: this.state.masterAttributeOptions?.find(
                    (e: any) => e.key == element.productAttributeId,
                  )?.dataType,
                  id: element?.id,
                  marketPlaceId: element?.marketPlaceId,
                  productDepartmentId: element?.productDepartmentId,
                }),
              );
            }
          });
          const response: any = await this.apiClient.addOrUpdateMarketplaceTemplateAttributes(
            value,
          );
          if (response.hasError) {
            this.notify.showErrorNotify(response.message);
          } else {
            this.state.btnSave
              ? this.notify.showSuccessNotify(this.props.t('NO_CHANGES_MADE'))
              : this.notify.showSuccessNotify(this.props.t('ATTRIBUTES_LINKED_TO_TEMPLATES'));
          }
          this.setState({ validatedData: [] }, () => {
            this.refresh();
          });
        }
      }
    } else {
      this.setState({ confirmModel: false });
      marketplaceAttributeNullCheck
        ? this.notify.showErrorNotify(this.props.t('MARKETPLACE_ATTRIBUTE_CANNOT_NULL'))
        : this.notify.showErrorNotify(this.props.t('DUPLICATE_ORDERS_FOUND'));
    }

    this.setState({
      rowIDs: [],
      AttributeValue: '',
      editedAttributes: { ...this.state.editedAttributes, [this.state.CurrentRowID]: undefined },
      editedColumnOrder: { ...this.state.editedColumnOrder, [this.state.CurrentRowID]: undefined },
    });
  }
  async removeTempalteAttri() {
    try {
      const selectedIds = [];
      for (let i = 0; i < this.state.selectedRowData.length; i++) {
        selectedIds.push(this.state.selectedRowData[i]?.id);
      }
      const response = await this.apiClient.removeMarketplaceTemplateAttributes(
        this.state.templateid,
        selectedIds,
      );
      let message: any =
        response.hasError == false ? `${this.props.t('MSG_RESET')}` : response.message;
      if (response.hasError) {
        this.notify.showErrorNotify(message);
      } else {
        this.notify.showSuccessNotify(message);
      }
      this.setState({ resetsave: [] });
      this.refresh();
    } catch (e: any) {
      this.notify.showErrorNotify(e);
    }
  }
  async FilterFun() {
    const MasterData = (await this.apiClient.getHierarchicalMasters()).result;
    const departmentSet = new Set();
    const departmentMap = new Map();
    MasterData?.forEach((element) => {
      if (this.divisionId.includes(String(element.division))) {
        const deptdata = {
          dept_Name: element.dept_Name,
          dept: element.dept,
          division: element.division,
        };
        const deptdropdown = {
          dept_Name: element.dept_Name,
          dept: element.dept,
        };
        const key = `${deptdata.dept_Name}_${deptdata.dept}_${deptdata.division}`;
        let isUnique = true;
        departmentMap.forEach((existingData: any) => {
          if (
            existingData.dept_Name === deptdata.dept_Name &&
            existingData.dept === deptdata.dept &&
            existingData.division === deptdata.division
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
    const departmentList = Array.from(departmentSet)
      .map((deptName: any) => ({
        text: deptName.dept_Name,
        key: deptName.dept,
      }))
      .sort((a: any, b: any) => {
        return a.text.localeCompare(b.text);
      });
    this.setState({
      departmentOption: departmentList,
      departmentOption1: Array.from(departmentMap.values()),
    });
  }
  onSearch(e: any) {
    var newValue = e?.target.value;
    newValue = newValue == undefined ? '' : newValue;
    this.setState({ searchValue: newValue });
    if (newValue !== '') {
      var searchTerm = newValue.trim().split(/ +/).join(' ').toLowerCase();

      var searchDataResult = this.state.searchData.filter((element: any) => {
        return this.state.masterAttributeOptions
          ?.find((e: any) => e.key == element.productAttributeId)
          ?.text.toLowerCase()
          .includes(searchTerm);
      });
      var result;
      if (searchDataResult.length > 0) {
        result = searchDataResult;
      } else {
        var validatedDataResult = this.state.validatedData.filter((element: any) => {
          return this.state.masterAttributeOptions
            ?.find((e: any) => e.key == element.productAttributeId)
            ?.text.toLowerCase()
            .includes(searchTerm);
        });

        result = validatedDataResult;
      }
      this.setState({ marketTemplateAttributeData: result });
      this.setState({
        marketTemplateAttributeData: result?.map((item: any, index: any) => ({
          index: index + 1,
          ...item,
        })),
      });
    } else {
      this.setState({ marketTemplateAttributeData: this.state.searchData });
      this.setState({
        marketTemplateAttributeData: this.state.searchData?.map((item: any, index: any) => ({
          index: index + 1,
          ...item,
        })),
      });
    }
  }

  updateRecord(data: any) {
    this.setState({ dataTypeError: false });
    for (var i = 0; i <= data?.length; i++) {
      // const masterDataType = this.state.masterAttributeOptions?.find(
      //   (e: any) => e.key == data[i]?.productAttributeId,
      // )?.dataType;
      if (data[i]?.marketplaceAttribute == null && data[i]?.datatype == null) {
      } else {
        const changedRows = [];
        for (let i = 0; i < this.state.marketTemplateAttributeData.length; i++) {
          const beforeUpdate = this.state.marketTemplateAttributeData[i];
          const afterUpdate = data[i];
          if (JSON.stringify(beforeUpdate) !== JSON.stringify(afterUpdate))
            changedRows.push(afterUpdate);
        }
        this.setState({ validatedData: changedRows });
      }
    }
  }
  resetColumnsData = () => {
    const { selectedRowData, marketTemplateAttributeData } = this.state;
    const updatedData = marketTemplateAttributeData.map((row: any) => {
      if (selectedRowData.some((selectedId: any) => selectedId.id === row.id)) {
        return {
          ...row,
          marketplaceAttribute: null,
          columnOrder: null,
        };
      }
      return row;
    });
    this.setState(
      {
        marketTemplateAttributeData: updatedData,
        btnSave: false,
        resetsave: selectedRowData,
      },
      () => {
        this.forceUpdate();
      },
    );
  };

  rowSelection = (e: any) => {
    this.setState({
      selectedRowData: e.selectedRows,
      selectionCount: e.selectedCount,
      selectedMarketAttri: e?.selectedRows[0]?.marketplaceAttributeId,
    });
    const ids = e.selectedRows.map((row: any) => row.id);
    this.setState({ rowIDs: ids });
  };

  UpdateAttributeMapping = (updateData: any, id: any, key: any, newValue: any) => {
    const objToUpdate = updateData.find((obj: any) => obj.id === id);
    if (objToUpdate) {
      objToUpdate[key] = newValue;
    }
    this.updateRecord(this.state.updateData);
    this.compareArrays(this.state.marketTemplateAttributeData, this.state.updateData);
  };
  compareArrays(obj1: any, obj2: any) {
    if (obj1 === obj2) {
      this.setState({ btnSave: true });
      return true;
    }
    if (!this.isObject(obj1) || !this.isObject(obj2)) {
      const result = this.normalizeValue(obj1) === this.normalizeValue(obj2);
      this.setState({ btnSave: result });
      return result;
    }
    const keys1 = Object.keys(obj1);
    const keys2 = Object.keys(obj2);
    if (keys1.length !== keys2.length) {
      this.setState({ btnSave: false });
      return false;
    }
    for (let key of keys1) {
      if (!obj2.hasOwnProperty(key) || !this.compareArrays(obj1[key], obj2[key])) {
        this.setState({ btnSave: false });
        return false;
      }
    }
    this.setState({ btnSave: true });
    return true;
  }
  async FilterFundept() {
    const MasterData = (await this.apiClient.getProductdeptMasters()).result;
    const departmentSet = new Set();
    const departmentMap = new Map();

    MasterData?.forEach((element) => {
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
    });
    // const departmentList = Array.from(departmentSet).map((deptName) => ({
    //   text: deptName,
    // }));
    const departmentList = Array.from(departmentSet)
      .map((deptName: any) => ({
        text: deptName.dept_Name,
        id: deptName.id,
        key: deptName.dept,
      }))
      .sort((a: any, b: any) => {
        return a.text.localeCompare(b.text);
      });
    this.setState({
      departmentOptionname: departmentList,
      departmentOption1name: Array.from(departmentMap.values()),
    });
  }

  isObject(value: any) {
    return typeof value === 'object' && value !== null;
  }
  normalizeValue(value: any) {
    if (value === null || value === undefined) {
      return '';
    }
    if (typeof value === 'string') {
      return value.trim();
    }
    return value;
  }
  ItemcodeTemplate = async () => {
    try {
      axios(
        baseUrl +
          `/api/Template/MarketPlaceDownload?marketplacetemplateid=${this.state.marketPlaceTemplateId}`,
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
          const url = `${baseUrl}/api/Template/MarketPlaceDownload?marketplacetemplateid=${this.state.marketPlaceTemplateId}`;
          downloadFileMarketPlace(url);
        }
      });
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
  };
  async onUpload(files: any) {
    try {
      if (!files) {
        return new Promise(() => {});
      }
      var f: any = files[0];
      this.setState({ uploadVisible: false, ProgressVisible: true, percentageComplete: f.name });
      const uri = (await this.apiClient.getAuthorizedUrlForWrite(f?.name || ''))?.result;
      var uploadData = new MarketPlaceUpload();
      uploadData.blobUrl = uri;
      uploadData.marketplacetemplateId = this.state.marketPlaceTemplateId;
      uploadData.divisioid = this.state.divisionid;
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
          this.apiClient.marketPlaceUpload(uploadData).then((r) => {
            if (r.hasError) {
              this.notify.showErrorNotify(
                r.message || this.props.t('MSG-DATA-FILEUPLOAD-COMMON-ERROR'),
              );
            } else {
              this.notify.showSuccessNotify(this.props.t('MSG-DATA-FILEUPLOAD-COMMON-SUCCESS'));
            }
            this.refresh();
          });
        });
    } catch (error) {
      this.setState({ ProgressVisible: false });
      this.notify.showErrorNotify(this.props.t('MSG-DATA-FILEUPLOAD-COMMON-ERROR'));
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
  hasAttributes = () => {
    if (this.state.selectedRowData.length === 0) {
      return false;
    }
    const selectreset = this.state.selectedRowData.every((row: any) => {
      if (row.marketplaceAttribute !== null || row.columnOrder !== null) {
        return true;
      } else {
        return false;
      }
    });
    return selectreset;

    // const selectreset = this.state.selectedRowData.some((row: any) => {
    //   if (row.datatype === 0) {
    //     return row.marketplaceAttribute !== null || row.datatype !== null;
    //   }
    // });
    // const selectreset1 = this.state.selectedRowData.every((row: any) => {
    //   if (row.datatype === 0) {
    //     return row.marketplaceAttribute !== null || row.datatype !== null;
    //   }
    // });
    // const selectreset2 = this.state.selectedRowData.some((row: any) => {
    //   if (row.datatype !== 0) {
    //     return row.marketplaceAttribute !== null || row.datatype !== null;
    //   }
    // });
    // const selectreset3 = this.state.selectedRowData.every((row: any) => {
    //   if (row.datatype !== 0) {
    //     return row.marketplaceAttribute !== null || row.datatype !== null;
    //   }
    // });
    // const selectreset4 = this.state.selectedRowData.some((row: any) => {
    //   if (
    //     (row.marketplaceAttribute !== null && row.datatype == null) ||
    //     (row.marketplaceAttribute == null && (row.datatype !== null || row.datatype === 0)) ||
    //     (row.marketplaceAttribute !== null && (row.datatype !== null || row.datatype === 0))
    //   ) {
    //     return true;
    //   }
    // });
    // const selectreset5 = this.state.selectedRowData.every((row: any) => {
    //   if (
    //     (row.marketplaceAttribute !== null && row.datatype == null) ||
    //     (row.marketplaceAttribute == null && (row.datatype !== null || row.datatype === 0)) ||
    //     (row.marketplaceAttribute !== null && (row.datatype !== null || row.datatype === 0))
    //   ) {
    //     return true;
    //   }
    // });

    // const selectreset6 = this.state.selectedRowData.some((row: any) => {
    //   if (row.datatype == null && row.marketplaceAttribute !== null) {
    //     return row.marketplaceAttribute !== null && row.datatype == null;
    //   }
    // });
    // const selectreset7 = this.state.selectedRowData.every((row: any) => {
    //   if (row.datatype == null && row.marketplaceAttribute !== null) {
    //     return row.marketplaceAttribute !== null && row.datatype == null;
    //   }
    // });

    // return (
    //   (selectreset && selectreset1) ||
    //   (selectreset2 && selectreset3) ||
    //   (selectreset4 && selectreset5) ||
    //   (selectreset6 && selectreset7)
    // );
  };
  customTheme = createTheme({
    palette: {
      themePrimary: 'red',
    },
  });

  InputDatacol = (id: any, value: any) => {
    const inputData = this.state.inputfield;
    inputData[`value${id}`] = value;
    this.setState({ inputfield: inputData });
  };

  InputDatacol1 = (id: any, value: any) => {
    const inputData = this.state.inputfield1;
    inputData[`value${id}`] = value;
    this.setState({ inputfield1: inputData });
  };

  showErrorNotify1 = () => {
    const errorPopup = document.createElement('div');
    errorPopup.className = 'error-popup';
    document.body.appendChild(errorPopup);
    setTimeout(() => {
      document.body.removeChild(errorPopup);
    }, 3000);
    this.notify.showErrorNotify(this.props.t('ERROR_ORDER_SHOULD_BE_IN_NUMBER'));
  };
  render() {
    const { t } = this.props;
    const columns: any = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{`${t('S_NO')}`}</div>,
        selector: (row: any) => row.index,
        width: '70px',
        isVisible: true,
      },
      {
        key: 'marketplaceId',
        name: <div className={columnHeader}>{`${t('MARKET_PLACE_ID')}`}</div>,
        selector: (row: any) => <span title={row.marketPlaceId}>{row.marketPlaceId}</span>,
        isVisible: true,
        width: '100px',
      },
      {
        key: 'marketplace',
        name: <div className={columnHeader}>{`${t('MARKET_PLACE')}`}</div>,
        selector: (row: any) => (
          <span title={this.state.MarketPlace?.find((e: any) => e.key == row.marketPlaceId)?.text}>
            {this.state.MarketPlace?.find((e: any) => e.key == row.marketPlaceId)?.text}
          </span>
        ),
        isVisible: true,
        width: '120px',
      },
      {
        key: 'productDepartmentId',
        name: <div className={columnHeader}>{`${t('COL_DEPARTMENT_ID')}`}</div>,
        selector: (row: any) => {
          if (
            this.state.marketTemplateAttributeData &&
            this.state.marketTemplateAttributeData.length > 0
          ) {
            const dept = this.state.departmentOptionname.find(
              (e: any) => e.id === row.productDepartmentId,
            );
            if (dept) {
              return <span title={dept.key}>{dept.key}</span>;
            } else {
              return null; // Or some default value if no match is found
            }
          }
          return null; // Or some default value if MarketPlace is undefined or empty
        },
        isVisible: true,
        width: '120px',
      },
      {
        key: 'productDepartment',
        name: <div className={columnHeader}>{`${t('COL_DEPARTMENT')}`}</div>,
        // selector: (row: any) => {
        //   const department = this.state.departmentOption?.find(
        //     (e: any) => String(e.key) === String(row.productDepartmentId),
        //   );
        //   return <span title={department?.text}>{department?.text}</span>;
        // },
        selector: (row: any) => {
          if (
            this.state.marketTemplateAttributeData &&
            this.state.marketTemplateAttributeData.length > 0
          ) {
            const dept = this.state.departmentOptionname.find(
              (e: any) => e.id === row.productDepartmentId,
            );
            if (dept) {
              return <span title={dept.text}>{dept.text}</span>;
            } else {
              return null; // Or some default value if no match is found
            }
          }
          return null; // Or some default value if MarketPlace is undefined or empty
        },
        isVisible: true,
        width: '140px',
      },
      {
        key: 'masterAttributesId',
        name: <div className={columnHeader}>{`${t('MASTER_ATTRIBUTES_ID')}`}</div>,
        selector: (row: any) => (
          <span title={row.productAttributeId}>{row.productAttributeId}</span>
        ),
        isVisible: true,
        width: '100px',
      },
      {
        key: 'masterAttributes',
        name: <div className={columnHeader}>{`${t('MASTER_ATTRIBUTES')}`}</div>,
        selector: (row: any) => (
          <span
            title={
              this.state.masterAttributeOptions?.find((e: any) => e.key == row.productAttributeId)
                ?.text
            }
          >
            {
              this.state.masterAttributeOptions?.find((e: any) => e.key == row.productAttributeId)
                ?.text
            }
          </span>
        ),
      },
      {
        key: 'dataType',
        width: '135px',
        name: <div className={columnHeader}>{`${t('DATA_TYPE')}`}</div>,
        selector: (row: any) => {
          const matchedAttributeOption = this.state.masterAttributeOptions?.find(
            (e: any) => e.key === row.productAttributeId,
          );

          let matchedText: any = '';
          let matchedDataTypeText: any = '';

          if (matchedAttributeOption) {
            const matchedDataType = this.state.dataType.find(
              (dataType: any) => dataType.key === matchedAttributeOption.dataType,
            );

            if (matchedDataType) {
              matchedText = matchedDataType.text;
              matchedDataTypeText = matchedDataType.text;
            }
          }

          return <span title={matchedDataTypeText}>{matchedText}</span>;
        },
      },
      {
        key: 'attributes',
        width: '175px',
        name: <div className={columnHeader}>{`${t('ATTRIBUTES')}`}</div>,
        selector: (row: any) => {
          return (
            <span title={row.marketplaceAttribute}>
              {row.marketplaceAttribute != null || row.marketplaceAttribute != undefined
                ? this.renderAttributesCell(row)
                : this.state.marketTemplateAttributeData.length !== 0 && (
                    <TextField
                      className="textField"
                      defaultValue={this.state.inputfield[`value${row.id}`]}
                      onChange={(e: any) => {
                        this.UpdateAttributeMapping(
                          this.state.updateData,
                          row.id,
                          'marketplaceAttribute',
                          e.target.value,
                        );
                        this.InputDatacol(row.id, e.target.value);
                      }}
                    />
                  )}
            </span>
          );
        },
        conditionalCellStyles: [
          {
            when: (row: any, column: any) => {
              return 1 == 1;
            },
            style: {
              backgroundColor: 'white',
            },
          },
        ],
      },

      {
        key: 'attributeOrder',
        width: '135px',
        name: <div className={columnHeader}>{`${t('ATTRIBUTE_ORDER')}`}</div>,
        selector: (row: any) => {
          const masterAttributeCount = this.state.masterAttributeOptions
            ? this.state.masterAttributeOptions.length
            : 0;
          return (
            <span title={row.columnOrder}>
              {row.columnOrder != null || row.columnOrder != undefined
                ? this.renderAttributeOrderCell(row)
                : this.state.marketTemplateAttributeData.length !== 0 && (
                    <>
                      {this.state.masterAttributeOptions && (
                        <TextField
                          className="textField"
                          defaultValue={this.state.inputfield1[`value${row.id}`]}
                          type="text"
                          onChange={(e: any) => {
                            const value = e.target.value;
                            this.UpdateAttributeMapping(
                              this.state.updateData,
                              row.id,
                              'columnOrder',
                              value,
                            );
                            this.InputDatacol1(row.id, e.target.value);
                          }}
                          onKeyPress={(e: any) => {
                            if (!/^\d*$/.test(e.key)) {
                              e.preventDefault();
                              this.showErrorNotify1();
                            } else if (
                              parseInt(e.target.value + e.key) > masterAttributeCount ||
                              parseInt(e.target.value + e.key) < 1
                            ) {
                              e.preventDefault();
                              this.notify.showErrorNotify(
                                this.props.t('MARKETPLACE_ORDER_WITHIN_RANGE') +
                                  `${masterAttributeCount}`,
                              );
                            }
                          }}
                          onPaste={(e: any) => {
                            const pastedData = e.clipboardData.getData('Text');
                            if (!/^\d*$/.test(pastedData)) {
                              e.preventDefault();
                              this.showErrorNotify1();
                            } else if (
                              parseInt(pastedData) > masterAttributeCount ||
                              parseInt(pastedData) < 1
                            ) {
                              e.preventDefault();
                              this.notify.showErrorNotify(
                                this.props.t('MARKETPLACE_ORDER_WITHIN_RANGE') +
                                  `${masterAttributeCount}`,
                              );
                            }
                          }}
                          onInput={(e: any) => {
                            const value = e.target.value;
                            if (
                              !/^\d*$/.test(value) ||
                              parseInt(value) > masterAttributeCount ||
                              parseInt(value) < 1
                            ) {
                              e.preventDefault();
                              this.showErrorNotify1();
                              this.notify.showErrorNotify(
                                this.props.t('MARKETPLACE_ORDER_WITHIN_RANGE') +
                                  `${masterAttributeCount}`,
                              );
                              e.target.value = value.replace(/[^\d]/g, '');
                            }
                          }}
                        />
                      )}
                    </>
                  )}
            </span>
          );
        },

        conditionalCellStyles: [
          {
            when: (row: any, column: any) => {
              return 1 == 1;
            },
            style: {
              backgroundColor: 'white',
            },
          },
        ],
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
          <Grid lg={12} item container spacing={2} direction={'row'}>
            <Grid item lg={8} xs={12}>
              {/* <Text block className="text">
              {`${t('PIM')} / ${t('OUTPUT')} /`}
              <Link to="/output/template">{`${t('TEMPLATES')}`}</Link>
              {` / ${t('ATTRIBUTES')}`}
            </Text> */}
              <Text variant="xxLarge" styles={PIMHearderText} className="color-blue text">
                {this.state.Title}
                {/* === '' ? `${this.props.t('LOADING')}` : this.state.Title */}
              </Text>
            </Grid>
            <Grid item lg={2.95} xs={6} />
            <Grid item lg={0.5} xs={6}>
              <Link
                to={`/configuration/CatalogMapping`}
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
            <Grid item lg={0.5} xs={6}>
              <Icon
                iconName="Download"
                title={t('BTN_DOWNLOAD')}
                className="iconStyle"
                onClick={() => {
                  if (this.state.marketTemplateAttributeData.length !== 0) {
                    this.ItemcodeTemplate();
                  }
                }}
              />
            </Grid>
          </Grid>
          <div>
            <Grid lg={12} item container spacing={2} direction={'row'}>
              <Grid item lg={2} xs={12}>
                <SearchBox
                  className="searchBox"
                  styles={PIMsearchBoxStyles}
                  placeholder={t('MENU_ATTRIBUTES')}
                  onChange={this.onSearch.bind(this)}
                  value={this.state.searchValue}
                  onClear={() => this.setState({ searchValue: '' })}
                />
              </Grid>
              <Grid item lg={1.2} xs={6}>
                <span title={t('BTN_RESET')}>
                  <Icon
                    iconName="RevToggleKey"
                    className="buttonIcon"
                    onClick={() => {
                      if (!this.hasAttributes()) {
                         return;
                      }
                      this.resetColumnsData();
                    }}
                    style={{
                      cursor: !this.hasAttributes() ? 'not-allowed' : 'pointer',
                      color: !this.hasAttributes() ? 'gray' : 'inherit',
                    }}
                  />
                </span>
              </Grid>
              <Grid item lg={0.45} xs={6}>
                <Icon
                  iconName="Save"
                  title={t('BTN_SAVE')}
                  className="iconStyle icon1"
                  onMouseOver={() => this.setState({ savingValue: true })}
                  onMouseOut={() => this.setState({ savingValue: false })}
                  onClick={() => {
                    // if (!this.state.btnSave) {
                    this.addAttributesTemp();
                    // }
                  }}
                />
              </Grid>
              <Grid item lg={0.45} xs={6}>
                <Icon
                  iconName="Upload"
                  title={`${t('BTN_UPLOAD_ATTRIBUTE')}`}
                  className="iconStyle icon1"
                  onClick={() => this.setState({ uploadVisible: true })}
                />
              </Grid>
              <Grid item lg={7.36} xs={12} />
              <Grid item lg={0.41} xs={12}>
                <Icon
                  iconName="Refresh"
                  title={t('BTN_REFRESH')}
                  className="iconStyle"
                  onClick={() => this.refresh()}
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
              {/* <div className={PIMcontentStyles.confirmHeader}>
                <IconButton
                  styles={iconButtonStyles}
                  iconProps={cancelIcon}
                  ariaLabel="Close popup modal"
                  onClick={() => {
                    this.setState({ confirmModel: false });
                  }}
                />
              </div> */}
              <div className={PIMcontentStyles.confirmbody}>
                <Text variant="xLarge" className="apptext1">{`${t(
                  'OUTPUT_TEMPLATE_ATTRIBUTE_CONFIRM_SAVE',
                )}`}</Text>
              </div>
              <div className={PIMcontentStyles.footer}>
                <Grid lg={12} item container spacing={2}>
                  <Grid item lg={4} xs={12}>
                    <DefaultButton
                      className="button"
                      styles={PIMfooterButtonConfirm}
                      text={`${t('BTN_CONFIRM')}`}
                      onClick={this.addAttributesTemp.bind(this)}
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
              isOpen={this.state.uploadVisible}
              containerClassName={PIMcontentStyles.ExcelContainer2}
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
                <IconButton
                  styles={iconButtonStyles}
                  iconProps={cancelIcon}
                  ariaLabel="Close popup modal"
                  onClick={() => {
                    this.setState({ uploadVisible: false });
                  }}
                />
              </div>
              <div className={PIMcontentStyles.body}>
                <UploadAttribute
                  ClosePopup={() => this.setState({ uploadVisible: false })}
                  UploadData={(file: any) => this.onUpload(file)}
                />
              </div>
            </Modal>
            <div className="dataTableFixedFooter">
              <DataTable
                columns={columns}
                data={this.state.marketTemplateAttributeData}
                onColumnOrderChange={handleColumnReorder}
                customStyles={customTableStyles1}
                pagination={true}
                selectableRows={this.state.selectedRowData}
                onSelectedRowsChange={this.rowSelection}
                clearSelectedRows={this.state.noSelection}
                fixedHeaderScrollHeight="68.5vh"
                selectableRowsHighlight
                // highlightOnHover
                responsive
                fixedHeader
                // striped
                theme="customTheme"
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
        </div>
      </div>
    );
  }
}
const ComponentTranslated = withTranslation()(OutputTemplateAttribute);
function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}
export default App;
