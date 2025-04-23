import {
  ComboBox,
  DefaultButton,
  Dropdown,
  IChoiceGroupOption,
  Icon,
  IconButton,
  IDropdownOption,
  Modal,
  SearchBox,
  Stack,
} from '@fluentui/react';
import { Grid } from '@mui/material';
import React from 'react';
import { Suspense } from 'react';
import { withTranslation } from 'react-i18next';
import { Text } from '@fluentui/react/lib/Text';
import {
  activeText,
  columnHeader,
  hyperlink,
  PIMButtons,
  PIMcontentStyles,
  PIMdropdownStyles,
  PIMdropdownStyles2,
  PIMfooterButtonCancel,
  PIMfooterButtonConfirm,
  PIMHearderText,
  PIMsearchBoxStyles,
} from '../../PimStyles';
import DataTable from 'react-data-table-component';
import ApiManager from '@/services/ApiManager';
import {
  AttributeStyles,
  CancelButtonStyles,
  cancelIcon,
  CancelStyles,
  clearFilter,
  columnFilter,
  filterButton,
  filterIcon,
  iconButtonStyles,
  UploadStyles,
} from '@/common/properties';
import AddOrUpdateAttribute from './AddOrUpdateAttribute';
import NotificationManager from '@/services/NotificationManager';
import axios from 'axios';
import { baseUrl } from '@/services/Constants';
import jwt_decode from 'jwt-decode';

class CreateAttribute extends React.Component<any, any> {
  apiClient = new ApiManager().CreateApiClient();
  token = sessionStorage.getItem('token');
  decode: any = jwt_decode(this.token || '');
  roleId: any = this.decode.RoleId.split(',').map(Number);
  notify = new NotificationManager();
  options: IDropdownOption[] = [
    { key: 2, text: 'SelectAll' },
    { key: 0, text: 'Yes' },
    { key: 1, text: 'No' },
  ];
  fieldType: any = [
    { key: true, text: this.props.t('MANDATORY') },
    { key: false, text: this.props.t('OPTIONAL') },
  ];

  isEdit: boolean = false;
  selectedDivision: any = 0;
  filterFieldType: any = [];
  filterDataType: any = [];

  constructor(props: any) {
    super(props);
    this.state = {
      attributeData: [],
      searchData: [],
      noSelection: false,
      selectedRowData: [],
      isRecord: false,
      selectionCount: 0,
      confirmModel: false,
      departmentOptions: [],
      assignDepartment: false,
      selectedDepartment: 0,
      isActive: true,
      divisionOptionlist: [],
      isAddOrUpdateModalVisible: false,
      selectedDivisionId: 0,
      isFieldFilter: false,
      isDataFilter: false,
      dataTypeSelected: null,
      fieldtypeSelected: null,
      searchKey: '',
      dataType: [],
      A_D_Visible1: false,
      filtervalue: 1,
      divisionSelected: '',
      divisionSelected1: '',
      downloadExcel: '',
      statusdropdown: '',
      statusdropdown1: '',
      selecteddivisionoptionflag: false,
    };
  }

  async componentDidMount() {
    this.setState({ isRecord: true });
    const appConfiguration: any = await this.apiClient.getAppConfig();
    const dataType = JSON.parse(
      appConfiguration?.result?.find((e: any) => e.name == 'DataType')?.jsonValue,
    );
    this.apiClient.getDivision().then((r: any) => {
      var divisiondatatype: any = [];
      r.result?.forEach((element: any) => {
        divisiondatatype.push({
          key: element.id,
          text: element.name,
        });
      });
      this.selectedDivision = divisiondatatype[0]?.key;
      this.setState({ divisionOptionlist: divisiondatatype, dataType: dataType });
      this.refresh(2);
    });
  }
  async refresh(isActive: number) {
    try {
      const dd =
        this.state.selecteddivisionoptionflag === true
          ? this.state.statusdropdown1
          : this.selectedDivision;
      this.setState({ isRecord: true, isActive: isActive });
      const attributeResponse = await this.apiClient.searchAttributes(isActive, dd);
      this.filterDataType = [];
      this.filterFieldType = [];
      this.setState({
        attributeData: attributeResponse.result,
        searchData: attributeResponse.result?.map((item: any, index: any) => ({
          index: index + 1,
          ...item,
        })),
        noSelection: !this.state.noSelection,
        selectedRowData: [],
        selectionCount: 0,
        isRecord: false,
        dataTypeSelected: null,
        fieldtypeSelected: null,
        isFieldFilter: false,
        isDataFilter: false,
        searchKey: '',
      });
      this.setState({
        attributeData: attributeResponse.result?.map((item: any, index: any) => ({
          index: index + 1,
          ...item,
        })),
      });
    } catch (e: any) {
      this.notify.showErrorNotify(e);
    }
  }

  downloadFile = () => {
    this.notify.showSuccessNotify1(this.props.t('DOWNLOAD_STARTED'));
    document.body.classList.add('loading-indicator');

    const divisionId =
      this.state.selecteddivisionoptionflag === true
        ? this.state.statusdropdown1
            .split(',')
            .map((value: any) => `'${value.trim()}'`)
            .join(',')
        : `'${this.selectedDivision}'`; // Wrap this.selectedDivision in single quotes

    const { downloadExcel } = this.state;
    let url =
      downloadExcel === true
        ? `${baseUrl}/api/Product/AttributesExportExcel?divisionId=${divisionId}&isActive=true`
        : downloadExcel === false
        ? `${baseUrl}/api/Product/AttributesExportExcel?divisionId=${divisionId}&isActive=false`
        : `${baseUrl}/api/Product/AttributesExportExcel?divisionId=${divisionId}`;

    axios({
      method: 'POST',
      url: url, // Corrected from _url to url
      responseType: 'blob',
      headers: {
        Token: this.token,
        'Content-Type': 'application/json;',
      },
    })
      .then((response: any) => {
        document.body.classList.remove('loading-indicator');
        this.notify.hideErrorNotify();
        this.notify.showSuccessNotify(this.props.t('DOWNLOAD_COMPLETED'));

        const fileUrl = window.URL.createObjectURL(new Blob([response.data]));
        const link = document.createElement('a');
        link.href = fileUrl;
        link.setAttribute('download', 'PIMAttributesExcel.xlsx');
        link.click();

        window.URL.revokeObjectURL(fileUrl);
      })
      .catch((error: any) => {
        document.body.classList.remove('loading-indicator');
        console.error('Error:', error);
        this.notify.showErrorNotify(error);
      });
  };

  async ActiveAttribute() {
    try {
      const selectedRecordIds = [];
      for (let i = 0; i < this.state.selectedRowData.length; i++) {
        selectedRecordIds.push(this.state.selectedRowData[i].id);
      }
      const allActive = this.state.selectedRowData.every((row: any) => !row.isActive);

      const response: any =
        this.state.isActive && allActive
          ? await this.apiClient.activateAttributes(selectedRecordIds)
          : await this.apiClient.deactivateAttributes(selectedRecordIds);
      let message =
        response.hasError == true
          ? response.message || `${this.props.t('ERROR_UNKNOWN')}`
          : this.state.isActive && allActive
          ? `${this.props.t('MSG_ACTIVATE')}`
          : `${this.props.t('MSG_DEACTIVATE')}`;
      this.setState({ confirmModel: false });
      this.notify.showSuccessNotify(message);
      this.refresh(2);
    } catch (e: any) {
      this.notify.showErrorNotify(e);
    }
    this.setState({ divisionSelected: '' });
    this.refresh(2);
  }

  rowSelection = (e: any) => {
    this.setState({ selectedRowData: e.selectedRows, selectionCount: e.selectedCount });
  };
  onChange = (ev: React.FormEvent<HTMLInputElement>, option: IChoiceGroupOption) => {
    this.setState({
      selectedDepartment: option.key,
      selectedDivisionId: option.key,
      noSelection: !this.state.noSelection,
      selectedRowData: [],
      selectionCount: 0,
      divisionSelected: '',
    });
    this.selectedDivision = option.key;
    this.refresh(2);
  };

  onAddOrEdit(isEditing: boolean) {
    this.isEdit = isEditing;
    this.setState({ isAddOrUpdateModalVisible: true });
  }

  onFormSubmit = (r: any, message: any) => {
    this.refresh(2);
    this.setState({ visible: false });
    var resMsg = !r.hasError ? r.message : r.result;
    !r.hasError ? this.notify.showSuccessNotify(message) : this.notify.showErrorNotify(message);
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
    this.setState({ attributeData: result });
    this.setState({
      attributeData: result?.map((item: any, index: any) => ({ index: index + 1, ...item })),
    });
  }
  async selectedOption(e: any) {
    this.setState({ isActive: e });
    this.refresh(e);
  }
  onColumnFilter = async () => {
    var { searchData } = this.state;
    var data: any = [];
    var filterData: any = [];
    if (this.filterDataType.length != 0) {
      var data = searchData.filter((element: any) => {
        return element.dataType == this.filterDataType;
      });
    } else {
      data = searchData;
    }
    if (this.filterFieldType.length != 0) {
      filterData = data.filter((element: any) => {
        return element.isRequired == this.filterFieldType;
      });
    } else {
      filterData = data;
    }
    this.setState({ attributeData: filterData });
  };
  handleRowClick = (row: any) => {
    this.isEdit = true;
    this.setState({
      isAddOrUpdateModalVisible: true,
      selectedRowData: [row],
    });
  };
  perpage = () => {
    const recordCont = [25, 50, 100, 150, 200];
    return recordCont;
  };
  Defaultperpage = () => {
    const recordCont = 25;
    return recordCont;
  };
  DeleteAttributes = async () => {
    this.setState({ A_D_Visible1: false });
    try {
      if (this.state.selectedRowData && this.state.selectedRowData.length > 0) {
        const ids = this.state.selectedRowData.map((e: any) => parseInt(e.id, 10));
        await this.apiClient.deleteAttribute(ids);
        this.notify.showSuccessNotify('Deleted successfully');
        this.refresh(2);
      }
    } catch (error) {
      console.error('Error deleting user:', error);
    }
    this.setState({ divisionSelected: '' });
    this.refresh(2);
  };
  handleDropdownChange = (
    event: React.FormEvent<HTMLDivElement>,
    option?: IDropdownOption,
    index?: number,
  ) => {
    const { options } = this;
    if (option) {
      let selectedKeys = [...this.state.divisionSelected1];

      if (option.key === 2) {
        if (option.selected) {
          selectedKeys = options.map((opt: IDropdownOption) => opt.key as string);
        } else {
          selectedKeys = [];
        }
      } else {
        if (option.selected) {
          if (!selectedKeys.includes(option.key as string)) {
            selectedKeys.push(option.key as string);
          }
        } else {
          selectedKeys = selectedKeys.filter((key) => key !== (option.key as string));
        }

        if (selectedKeys.includes(0) && selectedKeys.includes(1)) {
          if (!selectedKeys.includes(2)) {
            selectedKeys.push(2);
          }
        } else if (!selectedKeys.includes(0) || !selectedKeys.includes(1)) {
          selectedKeys = selectedKeys.filter((key) => key !== 2);
        }
      }

      const allOptionsSelected = options.length > 0 && selectedKeys.length === options.length;
      if (allOptionsSelected) {
        if (!selectedKeys.includes(2)) {
          selectedKeys = [2, ...selectedKeys];
        }
      } else {
        selectedKeys = selectedKeys.filter((key) => key !== 2);
      }
      const isActive = selectedKeys.includes(0) ? 0 : selectedKeys.includes(1) ? 1 : 2;

      this.setState(
        {
          divisionSelected1: selectedKeys,
          isActive: isActive,
          currentPage: 1,
          noSelection: true,
          filtervalue: isActive === 1 ? 0 : 1,
        },
        () => {
          const statusdropdown = selectedKeys.join(',');
          this.setState({ statusdropdown }, () => {
            const inActivePage = selectedKeys?.includes(2)
              ? 2
              : selectedKeys?.length === 1
              ? selectedKeys[0]
              : 2;
            this.refresh(inActivePage);
            let statusname;
            if (inActivePage === 2) {
              statusname = null;
            } else if (inActivePage === 1) {
              statusname = false;
            } else if (inActivePage === 0) {
              statusname = true;
            } else {
              statusname = null;
            }
            this.setState({ downloadExcel: statusname }, () => {});
          });
        },
      );
    }
  };
  onChangehandleDropdownChange = (event: any, option?: IDropdownOption, index?: number) => {
    this.setState({ selecteddivisionoptionflag: true });
    const { divisionOptionlist } = this.state;
    if (option) {
      let divselectedKeys = [...this.state.divisionSelected];

      if (option.key === 'select-toggle') {
        if (option.selected) {
          divselectedKeys = divisionOptionlist.map((opt: any) => opt.key as string);
        } else {
          divselectedKeys = [];
        }
      } else {
        if (option.selected) {
          divselectedKeys.push(option.key as string);
        } else {
          divselectedKeys = divselectedKeys.filter((key) => key !== option.key);
        }
      }
      const allOptionsSelected =
        divisionOptionlist?.length > 0 && divselectedKeys.length === divisionOptionlist?.length;
      if (allOptionsSelected) {
        if (!divselectedKeys.includes('select-toggle')) {
          divselectedKeys = ['select-toggle', ...divselectedKeys];
        }
      } else {
        divselectedKeys = divselectedKeys.filter((key) => key !== 'select-toggle');
      }
      const divisionId = divselectedKeys.filter((key) => key !== 'select-toggle').join(',') + ',';
      this.setState(
        {
          divisionSelected: divselectedKeys,
          isDivision: true,
          searchKey: '',
          divisionId,
        },
        () => {
          const selectedOptions = divisionOptionlist.filter((opt: any) =>
            divselectedKeys.includes(opt.key as string),
          );
          const selectedTexts = selectedOptions.map((opt: any) => opt.key);
          const statusdropdown1 = selectedTexts.join(',');
          this.setState({ statusdropdown1 }, () => {
            const statusofisactive =
              this.state.statusdropdown && this.state.statusdropdown.length < 3
                ? this.state.statusdropdown
                : 2;
            this.refresh(statusofisactive);
          });
        },
      );
    }
  };

  render() {
    const { t } = this.props;
    const columns = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{`${this.props.t('S_NO')}`}</div>,
        selector: (row: any) => row.index,
        isVisible: true,
        maxWidth: '200px',
      },
      {
        key: 'attributes',
        name: <div className={columnHeader}>{`${this.props.t('TABLE_ATTRIBUTES')}`}</div>,
        selector: (row: any) => row.name,
        cell: (row: any) => (
          <span className={hyperlink} onClick={() => this.handleRowClick(row)}>
            {row.name}
          </span>
        ),
        isVisible: true,
        maxWidth: '500px',
      },
      {
        key: 'datatype',
        name: (
          <Grid container direction={'row'}>
            <Grid item lg={8} xs={9}>
              <div className={columnHeader}>{this.props.t('DATA_TYPE')}</div>
            </Grid>
            <Grid item lg={2} xs={3}>
              <ComboBox
                options={this.state.dataType}
                onChange={(e: any, selectedOption: any) => {
                  this.filterDataType = selectedOption.key;
                  this.setState({ isDataFilter: true, searchKey: '' });
                  this.onColumnFilter();
                }}
                styles={columnFilter}
                selectedKey={this.state.dataTypeSelected}
                title={'Filter'}
                style={{ marginLeft: -55, marginTop: -5 }}
              />
              <IconButton
                style={{ marginLeft: -20 }}
                styles={filterButton}
                iconProps={filterIcon}
                title={'Filter'}
              />
            </Grid>
            <Grid item lg={1} xs={1}>
              {this.state.isDataFilter && (
                <IconButton
                  style={{ marginLeft: 4 }}
                  title={'Clear Filter'}
                  styles={filterButton}
                  iconProps={clearFilter}
                  onClick={() => {
                    this.filterDataType = [];
                    this.setState({ isDataFilter: false });
                    this.onColumnFilter();
                  }}
                />
              )}
            </Grid>
          </Grid>
        ),
        selector: (row: any) => {
          const dataTypeText = this.state.dataType.find((e: any) => e.key == row.dataType)?.text;
          return typeof dataTypeText === 'string' ? (
            <span title={dataTypeText}>{dataTypeText}</span>
          ) : (
            dataTypeText
          );
        },
        isVisible: true,
        width: '200px',
      },
      {
        key: 'fieldtype',
        name: (
          <Grid container direction={'row'}>
            <Grid item lg={8} xs={8}>
              <div className={columnHeader}>{this.props.t('FIELD_TYPE')}</div>
            </Grid>
            <Grid item lg={2} xs={3}>
              <ComboBox
                options={this.fieldType}
                onChange={(e: any, selectedOption: any) => {
                  this.filterFieldType = selectedOption.key;
                  this.setState({ isFieldFilter: true, searchKey: '' });
                  this.onColumnFilter();
                }}
                styles={columnFilter}
                title={'Filter'}
                selectedKey={this.state.fieldtypeSelected}
                style={{ marginLeft: -55, marginTop: -6 }}
              />
              <IconButton
                style={{ marginLeft: -18 }}
                styles={filterButton}
                title={'Filter'}
                iconProps={filterIcon}
                ariaLabel="Close popup modal"
              />
            </Grid>
            <Grid item lg={1} xs={1}>
              {this.state.isFieldFilter && (
                <IconButton
                  style={{ marginLeft: 6 }}
                  title={'Clear Filter'}
                  styles={filterButton}
                  iconProps={clearFilter}
                  onClick={() => {
                    this.filterFieldType = [];
                    this.setState({ isFieldFilter: false });
                    this.onColumnFilter();
                  }}
                />
              )}
            </Grid>
          </Grid>
        ),
        selector: (row: any) => (
          <span
            title={
              row.isRequired == true
                ? `${this.props.t('MANDATORY')}`
                : row.isRequired == false
                ? `${this.props.t('OPTIONAL')}`
                : ''
            }
          >
            {row.isRequired == true
              ? `${this.props.t('MANDATORY')}`
              : row.isRequired == false
              ? `${this.props.t('OPTIONAL')}`
              : ''}
          </span>
        ),
        // row.isRequired == true
        //   ? `${this.props.t('MANDATORY')}`
        //   : row.isRequired == false
        //   ? `${this.props.t('OPTIONAL')}`
        //   : '',
        isVisible: true,
        width: '200px',
      },
      {
        key: 'division',
        name: <div className={columnHeader}>{`${this.props.t('DIVISION')}`}</div>,
        selector: (row: any) => {
          const divisionOption = this.state.divisionOptionlist?.find(
            (e: any) => e.key === row.divisionId,
          );
          const divisionText = divisionOption ? divisionOption.text : '';
          return typeof divisionText === 'string' ? (
            <span title={divisionText}>{divisionText}</span>
          ) : (
            divisionText
          );
        },
        isVisible: true,
        maxwidth: '500px',
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

    const allActive = this.state.selectedRowData.every((row: any) => row.isActive);
    const allInactive = this.state.selectedRowData.every((row: any) => !row.isActive);

    return (
      <div className="layout width-100">
        <div className="bg-Color">
          <Grid lg={12} item container spacing={2} direction={'row'} style={{ marginLeft: '-8px' }}>
            <Grid item lg={10.5} xs={10}>
              {/* <Text block className="text">{`${t('HEADER_PIM')} / ${t('ATTRIBUTES')}`}</Text> */}
              <Text
                variant="xxLarge"
                styles={PIMHearderText}
                className="color-blue text"
              >{`${this.props.t('ATTRIBUTES')}`}</Text>
            </Grid>
            <Grid item lg={0.9} xs={0} />
            <Grid item lg={0.6} xs={1}>
              <Icon
                iconName="Download"
                className="iconStyle"
                title={this.props.t('BTN_DOWNLOAD')}
                onClick={() => {
                  // if (
                  //   this.state.statusdropdown.length !== 0 ||
                  //   this.state.statusdropdown1.length !== 0
                  // ) {
                  this.downloadFile();
                  // }
                }}
              />
            </Grid>
          </Grid>
          <Grid lg={12} item container spacing={2} direction={'row'}>
            <Grid item lg={2} xs={12}>
              <SearchBox
                className="searchBox"
                styles={PIMsearchBoxStyles}
                placeholder={`${this.props.t('ATTRIBUTE_SEARCH')}`}
                onChange={(e: any) => this.onSearch(e)}
                value={this.state.searchKey}
                onClear={() => this.setState({ searchKey: '' })}
              />
            </Grid>
            <Grid item lg={0.7} xs={12}>
              <Text className={activeText}>{t('ACTIVE')}</Text>
            </Grid>
            <Grid item lg={1} xs={12}>
              <Dropdown
                className="dropdown"
                placeholder="selectoption"
                onChange={this.handleDropdownChange}
                selectedKeys={this.state.divisionSelected1}
                options={this.options}
                styles={PIMdropdownStyles}
                multiSelect
              />
            </Grid>
            <Grid item lg={1.3} xs={12}>
              <span title={this.props.t('ROLE_ADD_BTN')}>
                <Icon
                  iconName="CircleAddition"
                  className="buttonIcon"
                  onClick={() => {
                    if (this.state.selectionCount < 1 && this.state.isActive) {
                      this.setState({ selectedRowData: [] });
                      this.onAddOrEdit(false);
                    }
                  }}
                  style={{
                    cursor:
                      this.state.selectionCount < 1 && this.state.isActive
                        ? 'pointer'
                        : 'not-allowed',
                    color:
                      this.state.selectionCount < 1 && this.state.isActive ? 'inherit' : 'gray',
                  }}
                />
              </span>
            </Grid>

            {/* <Grid item lg={1} xs={12}>
              <DefaultButton
                iconProps={{ iconName: 'Edit' }}
                styles={PIMButtons}
                className="button"
                text={`${this.props.t('BTN_EDIT')}`}
                onClick={() => this.onAddOrEdit(true)}
                disabled={this.state.selectionCount == 1 && this.state.isActive ? false : true}
              />
            </Grid> */}
            <Grid item lg={1.5} xs={12}>
              {this.state.isActive === 0 ? (
                <span
                  title={allInactive ? this.props.t('BTN_ACTIVATE') : this.props.t('BTN_DEACTIVE')}
                >
                  {' '}
                  <Icon
                    iconName={allInactive ? 'ActivateOrders' : 'DeactivateOrders'}
                    className="buttonIcon1"
                    onClick={() => {
                      if (this.state.selectionCount >= 1) {
                        this.setState({ confirmModel: true });
                      }
                    }}
                    style={{
                      cursor: this.state.selectionCount >= 1 ? 'pointer' : 'not-allowed',
                      color: this.state.selectionCount >= 1 ? 'inherit' : 'gray',
                    }}
                  />
                </span>
              ) : (
                <span
                  title={allInactive ? this.props.t('BTN_ACTIVATE') : this.props.t('BTN_DEACTIVE')}
                >
                  <Icon
                    iconName={allActive ? 'DeactivateOrders' : 'ActivateOrders'}
                    className="buttonIcon1"
                    onClick={() => {
                      if (this.state.selectionCount >= 1) {
                        this.setState({ confirmModel: true });
                      }
                    }}
                    style={{
                      cursor: this.state.selectionCount >= 1 ? 'pointer' : 'not-allowed',
                      color: this.state.selectionCount >= 1 ? 'inherit' : 'gray',
                    }}
                  />
                </span>
              )}
            </Grid>
            <Grid>
              {this.roleId.includes(1) && (
                <span title={this.props.t('DELETE_PIM_ATTRIBUTES')}>
                  <Icon
                    iconName="Delete"
                    className="buttonIcon7"
                    onClick={() => {
                      if (this.state.selectionCount >= 1) {
                        // this.DeleteAttributes();
                        this.setState({ A_D_Visible1: true });
                      }
                    }}
                    style={{
                      cursor: this.state.selectionCount >= 1 ? 'pointer' : 'not-allowed',
                      color: this.state.selectionCount >= 1 ? 'inherit' : 'gray',
                    }}
                  />
                </span>
              )}
            </Grid>
            <Grid item lg={1} xs={12}>
              <Text className={activeText}>{this.props.t('DIVISION')}:</Text>
            </Grid>
            <Grid item lg={1.5} xs={12}>
              <Dropdown
                placeholder={`${t('DROPDOWN_PLACEHOLDER')}`}
                options={[
                  {
                    key: 'select-toggle',
                    text: 'Select All',
                    selected:
                      this.state.divisionSelected?.length ===
                      (this.state.divisionOptionlist?.length || 0),
                  },
                  ...(this.state.divisionOptionlist || []).map((option: any) => ({
                    key: option.key,
                    text: option.text,
                    selected: this.state.divisionSelected.includes(option.key),
                  })),
                ]}
                multiSelect
                onChange={this.onChangehandleDropdownChange}
                selectedKeys={this.state.divisionSelected}
                styles={PIMdropdownStyles2}
              />
            </Grid>
            <Grid item lg={2.5} xs={12} />
            <Grid item lg={0.5} xs={12}>
              <Icon
                iconName="Refresh"
                title={t('BTN_REFRESH')}
                className="iconStyle"
                onClick={() => {
                  this.setState({ divisionSelected: '' }, () => {
                    this.refresh(2);
                  });
                }}
              />
            </Grid>
          </Grid>
          <Modal
            isOpen={this.state.confirmModel}
            containerClassName={PIMcontentStyles.confirmContaineruser1}
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
                ariaLabel="Close popup modal"
                onClick={() => {
                  this.setState({
                    confirmModel: false,
                    noSelection: !this.state.noSelection,
                    selectedRowData: [],
                    selectionCount: 0,
                    divisionSelected: '',
                  });
                  this.refresh(2);
                }}
              />
            </div>
            <div className={PIMcontentStyles.confirmbody4}>
              {this.state.isActive === 0 || allActive ? (
                <Text variant="large" className="apptext2 activatetxt2">{`${t(
                  'CONFIRM_DEACTIVATE',
                )}`}</Text>
              ) : (
                <Text variant="large" className="apptext2 activatetxt1">{`${t(
                  'CONFIRM_ACTIVATE',
                )}`}</Text>
              )}
            </div>
            <div className={PIMcontentStyles.footer}>
              <Grid lg={12} item container spacing={2}>
                <Grid item lg={1.5} xs={12} />
                <Grid item lg={4} xs={12}>
                  <DefaultButton
                    styles={PIMfooterButtonConfirm}
                    className="button"
                    text={`${t('BTN_CONFIRM')}`}
                    onClick={this.ActiveAttribute.bind(this)}
                  />
                </Grid>
                <Grid item lg={0.5} xs={12} />
                <Grid item lg={4} xs={12}>
                  <DefaultButton
                    styles={PIMfooterButtonCancel}
                    className="button"
                    text={`${t('BTN_CANCEL')}`}
                    onClick={() => {
                      this.setState({
                        confirmModel: false,
                        noSelection: !this.state.noSelection,
                        selectedRowData: [],
                        selectionCount: 0,
                        divisionSelected: '',
                      });
                      this.refresh(2);
                    }}
                  />
                </Grid>
                <Grid item lg={2} xs={12} />
              </Grid>
            </div>
          </Modal>
          <Modal
            isOpen={this.state.isAddOrUpdateModalVisible}
            containerClassName={PIMcontentStyles.attributeContainer1}
            onDismiss={() =>
              this.setState({
                isAddOrUpdateModalVisible: false,
              })
            }
          >
            <div className={PIMcontentStyles.header}>
              <div className="apptext1">
                {this.isEdit == false ? `${t('ADD_ATTRIBUTE')}` : `${t('EDIT_ATTRIBUTE')}`}
              </div>
              <IconButton
                styles={AttributeStyles}
                iconProps={cancelIcon}
                ariaLabel="Close popup modal"
                onClick={() => {
                  this.setState({ isAddOrUpdateModalVisible: false, divisionSelected: '' });
                  this.refresh(2);
                }}
              />
            </div>
            <div className={PIMcontentStyles.body}>
              <AddOrUpdateAttribute
                fields={this.state.selectedRowData}
                division={this.state.divisionOptionlist}
                ClosePopup={() => {
                  this.setState({
                    isAddOrUpdateModalVisible: false,
                    noSelection: !this.state.noSelection,
                    selectedRowData: [],
                    selectionCount: 0,
                    divisionSelected: '',
                  });
                  this.refresh(2);
                }}
                dataType={this.state.dataType}
                Response={this.onFormSubmit}
                dataRefresh={() => this.refresh(2)}
              />
            </div>
          </Modal>
          <Modal
            isOpen={this.state.A_D_Visible1}
            containerClassName={PIMcontentStyles.confirmContaineruser1}
            isBlocking={false}
            onDismiss={() =>
              this.setState({
                A_D_Visible1: false,
              })
            }
          >
            <div className={PIMcontentStyles.confirmHeaderUser1}>
              <IconButton
                styles={UploadStyles}
                iconProps={cancelIcon}
                ariaLabel="Close popup modal"
                onClick={() => {
                  this.setState({
                    A_D_Visible1: false,
                    noSelection: !this.state.noSelection,
                    selectedRowData: [],
                    selectionCount: 0,
                    divisionSelected: '',
                  });
                  this.refresh(2);
                }}
              />
            </div>
            <div className={PIMcontentStyles.confirmbody4}>
              <Text variant="xLarge" className="apptext2 dlttxt">{`${t('CONFIRM_DELETE')}`}</Text>
            </div>

            <div className={PIMcontentStyles.footer}>
              <Grid lg={12} item container spacing={2}>
                <Grid item lg={1.5} xs={12} />
                <Grid item lg={4} xs={12}>
                  <DefaultButton
                    className="button"
                    styles={PIMfooterButtonConfirm}
                    text={`${t('BTN_CONFIRM')}`}
                    onClick={this.DeleteAttributes.bind(this)}
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
                        A_D_Visible1: false,
                        noSelection: !this.state.noSelection,
                        selectedRowData: [],
                        selectionCount: 0,
                        divisionSelected: '',
                      });
                      this.refresh(2);
                    }}
                  />
                </Grid>
                <Grid item lg={2} xs={12} />
              </Grid>
            </div>
          </Modal>
          <div className="dataTableFixedFooter">
            <DataTable
              columns={columns}
              data={this.state.attributeData}
              noHeader={this.state.attributeData}
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
              fixedHeaderScrollHeight="69.1vh"
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
    );
  }
}

const ComponentTranslated = withTranslation()(CreateAttribute);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
