import {
  ComboBox,
  DefaultButton,
  Dropdown,
  IDropdownOption,
  Icon,
  IconButton,
  Modal,
  SearchBox,
  Stack,
} from '@fluentui/react';
import React, { Suspense } from 'react';
import { Text } from '@fluentui/react/lib/Text';
import Grid from '@mui/material/Grid';
import { withTranslation } from 'react-i18next';
import DataTable from 'react-data-table-component';
import ApiManager from '@/services/ApiManager';
import Util from '@/services/Util';
import NotificationManager from '@/services/NotificationManager';
import {
  columnHeader,
  PIMHearderText,
  PIMButtons,
  PIMcontentStyles,
  activeText,
  PIMfooterButtonCancel,
  PIMfooterButtonConfirm,
  hyperlink,
  PIMdropdownStyles,
  PIMsearchBoxStyles,
} from '../../PimStyles';
import AddStudio from './AddorUpdateStudio';
import {
  CancelButtonStyles,
  CancelStyles,
  CancelroleStyles,
  DeleteIcon,
  UploadStyles,
  cancelIcon,
  clearFilter,
  columnFilter,
  filterButton,
  filterIcon,
  iconButtonStyles,
} from '@/common/properties';
import { BlockBlobClient } from '@azure/storage-blob';
import { baseUrl } from '@/services/Constants';
import { downloadStudio } from '@/pages/commonFunctions';
import Uploadstudio from './UploadstudioCap';
import jwt_decode from 'jwt-decode';
import axios from 'axios';
class StudioCapacity extends React.Component<any, any> {
  util = new Util();
  apiClient = new ApiManager().CreateApiClient();
  notify = new NotificationManager();
  token: any = sessionStorage.getItem('token');
  decode: any = jwt_decode(this.token || '');
  roleId: any = this.decode.RoleId.split(',').map(Number);
  Divisiton: any = [];
  options: IDropdownOption[] = [
    { key: 2, text: 'SelectAll' },
    { key: 0, text: 'Yes' },
    { key: 1, text: 'No' },
  ];
  selectedDivision: any = 0;
  constructor(props: any) {
    super(props);
    this.util = new Util();
    this.apiClient = new ApiManager().CreateApiClient();
    this.notify = new NotificationManager();
    this.state = {
      rowData: [],
      isRecord: false,
      isNew: true,
      noSelection: false,
      enableEditButton: false,
      enableActiveButton: false,
      inActivePage: false,
      selectedRowData: [],
      selectionCount: 0,
      searchKey: '',
      columns: [],
      studiostatus: 2,
      statusOption: [],
      A_D_Visible: false,
      Visible: false,
      confirmModel: false,
      divisionOptionlist: [],
      selectedDropdownKey: 2,
      refreshKey: 0,
      copyRecord: [],
      A_D_Visible1: false,
      divisionSelectedone: '',
      paramterstatus: '',
      downloadFile: '',
    };
    this.token = sessionStorage.getItem('token');
  }

  async componentDidMount() {
    this.setState({
      isRecord: true,
    });
    this.refresh(!this.state.inActivePage);
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
      this.refresh(this.state.isActive);
    });
  }

  async refresh(inActivePage: boolean | null) {
    try {
      this.setState({ isRecord: true });
      if (inActivePage == null) {
        this.setState({
          refreshKey: this.state.refreshKey + 1,
          selectedDropdownKey: 2,
          inActivePage: null,
        });
      }
      const ks = localStorage.getItem('isactiveflag')?.toString();
      const apiResponse = await this.apiClient.getStudiosDetails(ks);
      localStorage.removeItem('isactiveflag');
      var studios = apiResponse.result;
      this.setState({
        paramterstatus: '',
        rowData: studios?.map((item: any, index: number) => ({ index: index + 1, ...item })),
        searchData: studios?.map((item: any, index: number) => ({ index: index + 1, ...item })),
        copyRecord: studios?.map((item: any, index: number) => ({ index: index + 1, ...item })),
      });
    } catch (e: any) {
      this.notify.showErrorNotify(e.message);
    }
    this.setState({
      studio: studios,
      isRecord: false,
      A_D_Visible: false,
      Visible: false,
      noSelection: !this.state.noSelection,
      enableEditButton: false,
      enableActiveButton: false,
      selectedRowData: [],
      isDivision: '',
      divisionSelected: 0,
    });
  }

  onSearch(e: any) {
    const newValue = e?.target.value || '';
    this.setState({ searchKey: newValue });

    const { copyRecord } = this.state;

    const searchResult = copyRecord.filter((element: any) => {
      const lowerCaseStudioName = element?.studioName?.toLowerCase() || '';
      return lowerCaseStudioName.includes(newValue.trim().toLowerCase());
    });

    this.setState({ rowData: searchResult });
  }

  handleToDateChange = (date: any) => {
    this.setState({ toDate: date });
  };
  handleMinDateChange = (date: any) => {
    this.setState({ minDate: date });
  };
  perpage = () => {
    const recordCont = [25, 50, 100, 150, 200];
    return recordCont;
  };
  Defaultperpage = () => {
    const recordCont = 25;
    return recordCont;
  };
  rowSelection = (e: any) => {
    this.setState({ selectedRowData: e.selectedRows });
    const selectionCount = e.selectedCount;

    switch (selectionCount) {
      case 0:
        this.setState({ enableEditButton: false, enableActiveButton: false });
        break;
      case 1:
        this.setState({ enableEditButton: true, enableActiveButton: true });
        break;
      default:
        this.setState({ enableEditButton: false, enableActiveButton: true });
        break;
    }
  };
  columnFilter = async () => {
    var finalData: any = [];
    if (this.Divisiton.length != 0) {
      finalData = this.state.searchData.filter((element: any) => {
        return element.divisionId == this.Divisiton;
      });
      this.setState({
        rowData: finalData,
      });
    } else {
      this.setState({
        rowData: this.state.searchData,
      });
    }
  };
  async onActivateClick() {
    try {
      const selectedRecordIds = [];
      for (let i = 0; i < this.state.selectedRowData.length; i++) {
        selectedRecordIds.push(this.state.selectedRowData[i].id);
      }
      const response =
        this.state.inActivePage || this.state.selectedRowData.some((row: any) => row.isActive)
          ? await this.apiClient.deactivateStudios(selectedRecordIds)
          : await this.apiClient.activateStudios(selectedRecordIds);
      if (response.hasError) {
        this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN'));
      } else {
        this.notify.showSuccessNotify(
          this.state.inActivePage || this.state.selectedRowData.some((row: any) => row.isActive)
            ? `${this.props.t('MSG_DEACTIVATE')}`
            : `${this.props.t('MSG_ACTIVATE')}`,
        );
      }
      // if (this.state.selectedDropdownKey == '0') {
      //   this.refresh(true);
      // } else if (this.state.selectedDropdownKey == '1') {
      //   this.refresh(false);
      // } else {
      //   this.refresh(null);
      // }
      switch (this.state.selectedDropdownKey) {
        case '0':
          this.refresh(true);
          break;
        case '1':
          this.refresh(false);
          break;
        default:
          this.refresh(null);
          break;
      }
    } catch (e: any) {
      this.notify.showErrorNotify(e.message);
    }
    this.setState({divisionSelectedone:''})
    this.refresh(null);
  }
  handleRowDeselection = (id: any) => {
    this.setState({
      selectedRowData: this.state.selectedRowData.filter((r: any) => r.id !== id),
      selectionCount: this.state.selectedRowData.filter((r: any) => r.id !== id).length,
    });
  };
  handleRowClick = (row: any) => {
    this.setState({
      A_D_Visible: true,
      selectedRowData: [row],
    });
  };
  Deletestudio = async () => {
    this.setState({ A_D_Visible1: false });
    try {
      if (this.state.selectedRowData && this.state.selectedRowData.length > 0) {
        const ids = this.state.selectedRowData.map((e: any) => parseInt(e.id, 10));
        await this.apiClient.deleteStudio(ids);
        this.notify.showSuccessNotify('Deleted successfully');
        this.refresh(!this.state.inActivePage);
      }
    } catch (error) {
      console.error('Error deleting user:', error);
    }
    this.setState({divisionSelectedone:''})
    this.refresh(null);
  };
  download() {
    const filename = 'Studio';
    const token = this.token;
    const { downloadExcel } = this.state;

    // Build the base URL
    let url = `${baseUrl}/api/Studio/DownloadStudios?isactive=`;

    // Add isactive parameter based on downloadExcel state
    if (downloadExcel === true) {
      url += 'true';
    } else if (downloadExcel === false) {
      url += 'false';
    }

    // Append the token parameter
    url += `&token=${encodeURIComponent(token)}`;

    try {
      downloadStudio(url, filename);
    } catch (error: any) {
      this.notify.showErrorNotify(error);
    }
  }

  removeParentheses = (fileName: any) => {
    return fileName.replace(/\([^()]*\)/g, '');
  };

  async onUpload(files: any) {
    try {
      if (!files) {
        return new Promise(() => {});
      }
      const studioname = 'Studio';
      var f: any = files[0];
      const fileName =
        this.removeParentheses(f?.name).split('.').slice(0, -1).join('.').trim() + '.xlsx';

      if (
        !this.state.isNew &&
        this.state.selectedRowUploadFile?.name?.trim()?.toLowerCase() !==
          fileName?.trim()?.toLowerCase()
      ) {
        this.setState({ visible: false }, () => {
          this.notify.showErrorNotify(`${this.props.t('UPLOAD_DOWNLOADED_FILE-ERROR_DIFF_FILE')}`);
          this.setState({ uploadVisible: false });
        });
        return false;
      }

      const templateName =
        this.removeParentheses(studioname + '.xlsx')
          .split('.')
          .slice(0, -1)
          .join('.')
          .trim() + '.xlsx';

      const baseTemplateName = templateName.replace('.xlsx', '').trim();
      const validTemplatePattern = new RegExp(`^${baseTemplateName}(?:[- \\w]*)\\.xlsx$`, 'i');
      if (this.state.isNew && !validTemplatePattern.test(fileName)) {
        this.setState({ visible: false }, () => {
          this.notify.showErrorNotify(
            `${this.props.t('UPLOAD_DOWNLOADED_TEMPLATE-ERROR_DIFF_TEMPLATE')}`,
          );
          this.setState({ uploadVisible: false });
        });
        return false;
      }
      this.setState({ uploadVisible: false, ProgressVisible: true, percentageComplete: f.name });
      const uri = (await this.apiClient.getAuthorizedUrlForWrite(f?.name || ''))?.result;
      const blobUrl = uri;
      const blobClient = new BlockBlobClient(uri || '');
      blobClient
        .uploadData(f, {
          onProgress: (observer: any) => {
            var progress = Math.round(observer.loadedBytes / f.size);
            this.setState({ progressPercent: progress });
          },
          blobHTTPHeaders: {
            blobContentType: f.type,
          },
        })
        .then(async (blobClientResponse) => {
          this.setState({ ProgressVisible: false });
          this.apiClient.uploadstudioCapacity(blobUrl).then((r) => {
            if (r.hasError) {
              this.notify.showErrorNotify(
                r.message || this.props.t('MSG-DATA-FILEUPLOAD-COMMON-ERROR'),
              );
            } else {
              this.notify.showSuccessNotify(this.props.t('MSG-DATA-FILEUPLOAD-COMMON-SUCCESS'));
            }
            this.refresh(null);
          });
        });
    } catch (error) {
      this.setState({ ProgressVisible: false });
      this.notify.showErrorNotify(this.props.t('MSG-DATA-FILEUPLOAD-COMMON-ERROR'));
    }
  }
  handleDropdownChange = (
    event: React.FormEvent<HTMLDivElement>,
    option?: IDropdownOption,
    index?: number,
  ) => {
    const { options } = this;

    if (option) {
      let selectedKeys = [...this.state.divisionSelectedone];

      if (option.key === 2) {
        // Handle "Select All"
        if (option.selected) {
          selectedKeys = options.map((opt: IDropdownOption) => opt.key as string);
        } else {
          selectedKeys = [];
        }
      } else {
        // Handle individual options
        if (option.selected) {
          if (!selectedKeys.includes(option.key as string)) {
            selectedKeys.push(option.key as string);
          }
        } else {
          selectedKeys = selectedKeys.filter((key) => key !== (option.key as string));
        }

        // Toggle logic for 0 and 1
        if (selectedKeys.includes(0) && selectedKeys.includes(1)) {
          if (!selectedKeys.includes(2)) {
            selectedKeys.push(2);
          }
        } else {
          selectedKeys = selectedKeys.filter((key) => key !== 2);
        }
      }

      // Update the state based on selection changes
      this.setState(
        {
          divisionSelectedone: selectedKeys,
          refreshKey: this.state.refreshKey + 1, // Trigger re-render
        },
        () => {
          if (selectedKeys.length > 0) {
            const statusdropdown = selectedKeys.join(',');
            const inActivePage = selectedKeys.includes(2)
              ? 2
              : selectedKeys.length === 1
              ? selectedKeys[0]
              : 2;
            this.setState({ paramterstatus: inActivePage });
            const lc = localStorage.setItem('isactiveflag', inActivePage);
            this.refresh(inActivePage);
            let statusname;
            let ks = localStorage.getItem('isactiveflag')?.toString();
            if (ks === '2') {
              statusname = null;
            } else if (ks === '1') {
              statusname = false;
            } else if (ks === '0') {
              statusname = true;
            } else {
              statusname = null;
            }
            this.setState({ downloadExcel: statusname }, () => {});
          }
        },
      );
    }
  };

  render() {
    const { t } = this.props;
    var _column: any = [
      {
        key: 'indexColumn',
        name: <div className={columnHeader}>{`${this.props.t('S_NO')}`}</div>,
        selector: (row: any) => row.index,
        isVisible: true,
        maxWidth: '80px',
      },
      {
        key: 'studioname',
        name: <div className={columnHeader}>{`${this.props.t('COL_STUDIO_NAME')}`}</div>,
        selector: (row: any) => <span title={row.studioName}>{row.studioName}</span>,
        cell: (row: any) => (
          <span className={hyperlink} onClick={() => this.handleRowClick(row)}>
            {row.studioName}
          </span>
        ),
        isVisible: true,
        minWidth: '80px',
      },
      {
        key: 'studiocode',
        name: <div className={columnHeader}>{`${this.props.t('COL_STUDIO_CODE')}`}</div>,
        selector: (row: any) => <span title={row.code}>{row.code}</span>,
        isVisible: true,
        minWidth: '80px',
      },
      {
        key: 'division',
        name: (
          <Grid container direction={'row'}>
            <Grid item lg={8} xs={9} style={{ marginTop: '12px' }}>
              <div className={columnHeader}>{this.props.t('PLANOGRAM_TABLE_DIVISION')}</div>
            </Grid>
            <Grid item lg={2} xs={3}>
              <ComboBox
                className="dropdown"
                options={this.state.divisionOptionlist}
                onChange={(e: any, selectedOption: any) => {
                  this.setState({
                    isDivision: true,
                    divisionSelected: selectedOption.key,
                    searchKey: '',
                  });
                  this.Divisiton = selectedOption.key;
                  this.columnFilter();
                }}
                selectedKey={this.state.divisionSelected}
                styles={columnFilter}
                style={{ marginLeft: -20, marginTop: '10px' }}
              />
              <IconButton
                style={{ marginLeft: -20, marginTop: '3%' }}
                styles={filterButton}
                iconProps={filterIcon}
                title={'Filter'}
                ariaLabel="Close popup modal"
              />
            </Grid>
            <Grid item lg={1} xs={1}>
              {this.state.isDivision && (
                <IconButton
                  style={{ marginLeft: 10, marginTop: '3%' }}
                  title={'Clear Filter'}
                  styles={filterButton}
                  iconProps={clearFilter}
                  onClick={() => {
                    this.Divisiton = [];
                    this.setState({ isDivision: false, divisionSelected: 0 });
                    this.columnFilter();
                  }}
                />
              )}
            </Grid>
          </Grid>
        ),
        selector: (row: any) => {
          if (row.divisionId) {
            var division = this.state.divisionOptionlist.find((e: any) => e.key === row.divisionId);
            if (division) {
              return <span title={division.text}>{division.text}</span>;
            } else {
              return null;
            }
          }
        },
        position: 'fixed',
        isVisible: true,
        width: '140px',
      },
      {
        key: 'city',
        name: <div className={columnHeader}>{`${this.props.t('COL_CITY')}`}</div>,
        selector: (row: any) => <span title={row.city}>{row.city}</span>,
        isVisible: true,
        minWidth: '60px',
      },
      {
        key: 'priority',
        name: <div className={columnHeader}>{`${this.props.t('COL_PRIORITY')}`}</div>,
        selector: (row: any) => <span title={row.priority}>{row.priority}</span>,
        isVisible: true,
        minWidth: '60px',
      },
      {
        key: 'totalCapacity',
        name: <div className={columnHeader}>{`${this.props.t('COL_TOTAL_CAPACITY')}`}</div>,
        selector: (row: any) => <span title={row.totalCapacity}>{row.totalCapacity}</span>,
        isVisible: true,
        minWidth: '80px',
      },
      {
        key: 'assigned',
        name: <div className={columnHeader}>{`${this.props.t('ASSIGNED')}`}</div>,
        // selector: (row: any) => <span title={row.assigned}>{row.assigned}</span>,
        selector: (row: any) => {
          if (row.totalCapacity != null && row.remainingCapacity != null) {
            const assigned = row.totalCapacity - row.remainingCapacity;
            return (
              <span title={assigned.toString()}>{assigned}</span>
            );
          }
        },
        isVisible: false,
        minWidth: '100px',
      },
      {
        key: 'RemaningCapacity',
        name: <div className={columnHeader}>{`${this.props.t('COL_REMANING_CAPACITY')}`}</div>,
        selector: (row: any) => <span title={row.remainingCapacity}>{row.remainingCapacity}</span>,
        isVisible: true,
        minWidth: '170px',
      },
    ];
    const handleColumnReorder1 = (newColumns: any) => {
      var _columnss: any = [..._column];
      if (newColumns == this.state.currentColumns) {
        this.setState({ currentColumns: _columnss });
      } else {
        this.setState({ currentColumns: newColumns });
      }
    };
    return (
      <div className="layout width-100">
        <Stack>
          <Grid container spacing={2} direction="row" alignItems="center" className="grid">
            <Grid item lg={11.3} xs={10.4}>
              <Text variant="xxLarge" styles={PIMHearderText} className="color-blue text">{`${t(
                'MENU_STUDIO_CAPACITY',
              )}`}</Text>
            </Grid>
            <Grid item lg={0.01} xs={1} />
            <Grid item lg={0.4} xs={1}>
              <Icon
                iconName="Download"
                className="iconStyle"
                title={this.props.t('BTN_DOWNLOAD')}
                onClick={() => {
                  if (this.state.rowData.length !== 0) {
                    this.download();
                  }
                }}
              />
            </Grid>
          </Grid>
        </Stack>
        <Grid lg={12} item container spacing={2} direction={'row'}>
          <Grid item lg={2} xs={12}>
            <SearchBox
              name="search"
              className="searchBox"
              styles={PIMsearchBoxStyles}
              placeholder={`${t('COL_STUDIO_NAME')}`}
              onChange={(e: any) => this.onSearch(e)}
              value={this.state.searchKey}
              onClear={() => this.setState({ searchKey: '' })}
            />
          </Grid>
          <Grid item lg={1.8} xs={12}>
            <span title={t('ADD_STUDIO')}>
              <Icon
                iconName="CircleAddition"
                className="buttonIcon"
                onClick={() => {
                  if (this.state.selectedRowData.length !== 0) {
                    return;
                  }
                  this.setState({ A_D_Visible: true });
                }}
                style={{
                  cursor: this.state.selectedRowData.length !== 0 ? 'not-allowed' : 'pointer',
                  color: this.state.selectedRowData.length !== 0 ? 'gray' : 'inherit',
                }}
              />
            </span>
          </Grid>

          <Grid item lg={1.4} xs={12}>
            <span
              title={
                this.state.inActivePage ||
                this.state.selectedRowData.some((row: any) => row.isActive)
                  ? t('BTN_DEACTIVATE')
                  : t('BTN_ACTIVATE')
              }
            >
              <Icon
                iconName={
                  this.state.inActivePage ||
                  this.state.selectedRowData.some((row: any) => row.isActive)
                    ? 'DeactivateOrders'
                    : 'ActivateOrders'
                }
                className="buttonIcon4"
                onClick={() => {
                  if (
                    !this.state.enableActiveButton ||
                    !(
                      this.state.selectedRowData.every((row: any) => row.isActive) ||
                      this.state.selectedRowData.every((row: any) => !row.isActive) ||
                      this.state.selectedRowData.length === 0
                    )
                  ) {
                    return;
                  }
                  this.setState({
                    Visible: true,
                    // enableEditButton: false,
                    // enableActiveButton: false,
                    // seletedvalue: this.state.selectedRowData,
                  });
                }}
                style={{
                  cursor:
                    !this.state.enableActiveButton ||
                    !(
                      this.state.selectedRowData.every((row: any) => row.isActive) ||
                      this.state.selectedRowData.every((row: any) => !row.isActive) ||
                      this.state.selectedRowData.length === 0
                    )
                      ? 'not-allowed'
                      : 'pointer',
                  color:
                    !this.state.enableActiveButton ||
                    !(
                      this.state.selectedRowData.every((row: any) => row.isActive) ||
                      this.state.selectedRowData.every((row: any) => !row.isActive) ||
                      this.state.selectedRowData.length === 0
                    )
                      ? 'gray'
                      : 'inherit',
                }}
              />
            </span>
            <Grid item lg={1.4} xs={12} />
          </Grid>
          <Grid>
            {this.roleId.includes(1) && (
              <span title={this.props.t('DELETE_STUDIO')}>
                <Icon
                  iconName="Delete"
                  className="buttonIcon11 deletebutton"
                  // onClick={() => {
                  //   if (
                  //     !this.state.enableActiveButton ||
                  //     !(
                  //       this.state.selectedRowData.every((row: any) => row.isActive) ||
                  //       this.state.selectedRowData.every((row: any) => !row.isActive) ||
                  //       this.state.selectedRowData.length === 0
                  //     )
                  //   ) {
                  //     return;
                  //   }
                  //   this.setState({
                  //     A_D_Visible1: true,
                  //   });
                  // }}
                  // style={{
                  //   cursor:
                  //     !this.state.enableActiveButton ||
                  //     !(
                  //       this.state.selectedRowData.every((row: any) => row.isActive) ||
                  //       this.state.selectedRowData.every((row: any) => !row.isActive) ||
                  //       this.state.selectedRowData.length === 0
                  //     )
                  //       ? 'not-allowed'
                  //       : 'pointer',
                  //   color:
                  //     !this.state.enableActiveButton ||
                  //     !(
                  //       this.state.selectedRowData.every((row: any) => row.isActive) ||
                  //       this.state.selectedRowData.every((row: any) => !row.isActive) ||
                  //       this.state.selectedRowData.length === 0
                  //     )
                  //       ? 'gray'
                  //       : 'inherit',
                  // }}
                  onClick={() => {
                    if (!this.state.enableActiveButton || this.state.selectedRowData.length === 0) {
                      return;
                    }
                    this.setState({
                      A_D_Visible1: true,
                    });
                  }}
                  style={{
                    cursor:
                      !this.state.enableActiveButton || this.state.selectedRowData.length === 0
                        ? 'not-allowed'
                        : 'pointer',
                    color:
                      !this.state.enableActiveButton || this.state.selectedRowData.length === 0
                        ? 'gray'
                        : 'inherit',
                  }}
                />
              </span>
            )}
          </Grid>
          <Grid item lg={0.7} xs={6}>
            <Text className={activeText}>{t('ACTIVE')}</Text>
          </Grid>
          <Grid item lg={1.3} xs={6}>
            <Dropdown
              className="dropdown"
              key={this.state.refreshKey}
              placeholder="SelectOption"
              onChange={this.handleDropdownChange}
              selectedKeys={this.state.divisionSelectedone} // Ensure this prop is used for multi-select
              options={this.options}
              styles={PIMdropdownStyles}
              multiSelect
            />
          </Grid>
          <Grid item lg={0.45} xs={6}>
            <Icon
              iconName="Upload"
              title={`${t('BTN_UPLOAD_ATTRIBUTE')}`}
              className="iconStyle"
              onClick={() => this.setState({ uploadVisible: true })}
            />
          </Grid>
          <Grid item lg={3.32} xs={2} />
          <Grid item lg={0.5} xs={1}>
            <Icon
              iconName="Clear"
              title={t('BTN_CLEAR')}
              className="iconStyle"
              onClick={() => {
                const { inActivePage, searchKey, divisionSelectedone } = this.state;

                  if (inActivePage !== null || searchKey !== '' || divisionSelectedone !== '') {
                  this.setState(
                    { searchKey: '', studiostatus: 2, divisionSelectedone: '', inActivePage: null },
                    () => {
                      this.refresh(null);
                    }
                  );
                }
              }}
            />
          </Grid>
          <Grid item lg={0.5} xs={2}>
            <Icon
              iconName="Refresh"
              title={t('BTN_REFRESH')}
              className="iconStyle"
              onClick={() => {
                this.setState({ searchKey: '', studiostatus: 2, divisionSelectedone:'' }, () => {
                  this.refresh(null);
                });
              }}
            />
          </Grid>
        </Grid>
        <Modal
          isOpen={this.state.Visible}
          containerClassName={PIMcontentStyles.confirmContaineruser1}
          isBlocking={false}
          onDismiss={() =>
            this.setState({
              Visible: false,
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
                  Visible: false,
                  noSelection: !this.state.noSelection,
                  enableEditButton: false,
                  enableActiveButton: false,
                  selectedRowData: [],
                  divisionSelectedone:''
                });
                this.refresh(null);
              }}
            />
          </div>
          <div className={PIMcontentStyles.confirmbody4}>
            {this.state.inActivePage ||
            this.state.selectedRowData.some((row: any) => row.isActive) ? (
              <Text variant="xLarge" className="apptext2 activatetxt">{`${t(
                'CONFIRM_DEACTIVATE',
              )}`}</Text>
            ) : (
              <Text variant="xLarge" className="apptext2 activatetxt1">{`${t(
                'CONFIRM_ACTIVATE',
              )}`}</Text>
            )}{' '}
          </div>
          <div className={PIMcontentStyles.footer}>
            <Grid lg={12} item container spacing={2}>
              <Grid item lg={1.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonConfirm}
                  text={`${t('BTN_CONFIRM')}`}
                  onClick={this.onActivateClick.bind(this)}
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
                      Visible: false,
                      noSelection: !this.state.noSelection,
                      enableEditButton: false,
                      enableActiveButton: false,
                      selectedRowData: [],
                      divisionSelectedone:''
                    });
                    this.refresh(null);
                  }}
                />
              </Grid>
              <Grid item lg={2} xs={12} />
            </Grid>
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
                  enableEditButton: false,
                  enableActiveButton: false,
                  selectedRowData: [],
                  divisionSelectedone:''
                });
                this.refresh(null);
              }}
            />
          </div>
          <div className={PIMcontentStyles.confirmbody4}>
            <Text variant="xLarge" className="apptext2 dlttxt dlttxt1">{`${t('CONFIRM_DELETE')}`}</Text>
          </div>
          <div className={PIMcontentStyles.footer}>
            <Grid lg={12} item container spacing={2}>
              <Grid item lg={1.5} xs={12} />
              <Grid item lg={4} xs={12}>
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonConfirm}
                  text={`${t('BTN_CONFIRM')}`}
                  onClick={this.Deletestudio.bind(this)}
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
                      enableEditButton: false,
                      enableActiveButton: false,
                      selectedRowData: [],
                      divisionSelectedone:''
                    });
                    this.refresh(null);
                  }}
                />
              </Grid>
              <Grid item lg={2} xs={12} />
            </Grid>
          </div>
        </Modal>
        <Modal
          isOpen={this.state.A_D_Visible}
          containerClassName={PIMcontentStyles.confirmContainerStudio}
          isBlocking={false}
          onDismiss={() =>
            this.setState({
              A_D_Visible: false,
              selectedRowData: [],
            })
          }
        >
          <IconButton
            styles={iconButtonStyles}
            iconProps={{ iconName: 'Cancel' }}
            ariaLabel="Close popup modal"
            onClick={() => {
              this.setState({
                A_D_Visible: false,
                noSelection: !this.state.noSelection,
                enableEditButton: false,
                enableActiveButton: false,
                selectedRowData: [],
                divisionSelectedone:''
              })
              this.refresh(null);
            }
            }
          />
          <div className={PIMcontentStyles.header}>
            <div className="apptext1">
              {this.state.selectedRowData.length != 0
                ? `${this.props.t('EDIT_STUDIO')}`
                : `${this.props.t('ADD_STUDIO')}`}
            </div>
          </div>

          <AddStudio
            division={this.state.divisionOptionlist}
            t={this.props.t}
            fields={this.state.selectedRowData}
            studio={this.state.studio}
            ActionValues={this.state.inActivePage}
            selectedDropdownKey={this.state.selectedDropdownKey}
            ClosePopup={() => {
              this.setState({
                A_D_Visible: false,
                noSelection: !this.state.noSelection,
                enableEditButton: false,
                enableActiveButton: false,
                selectedRowData: [],
                divisionSelectedone:''
              });
              this.refresh(null);
            }}
            dataRefresh={() =>
              this.setState({ studiostatus: 2 }, () => {
                this.refresh(null);
              })
            }
            clearSearch={() => {
              this.setState({
                searchKey: '',
              });
            }}
          />
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
            <Uploadstudio
              ClosePopup={() => this.setState({ uploadVisible: false })}
              UploadData={(file: any) => this.onUpload(file)}
            />
          </div>
        </Modal>
        <div className="dataTableFixedFooter">
          <DataTable
            columns={_column}
            data={this.state.rowData}
            pagination={true}
            selectableRows={this.state.selectedRowData}
            onSelectedRowsChange={this.rowSelection}
            onColumnOrderChange={handleColumnReorder1}
            clearSelectedRows={this.state.noSelection}
            responsive
            striped
            highlightOnHover
            selectableRowsHighlight
            fixedHeader
            fixedHeaderScrollHeight="68.2vh"
            paginationComponentOptions={{ rowsPerPageText: `${this.props.t('ROWS_PER_PAGE')}` }}
            paginationPerPage={this.Defaultperpage()}
            paginationRowsPerPageOptions={this.perpage()}
            noDataComponent={
              <div className="noDataWidth">
                {<DataTable columns={_column} data={[{ '': '' }]} />}
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

const ComponentTranslated = withTranslation()(StudioCapacity);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
