import React, { Suspense } from 'react';
import 'react-phone-input-2/lib/style.css';
import { SearchBox } from '@fluentui/react/lib/SearchBox';
import DataTable from 'react-data-table-component';

import { ITextProps, Text } from '@fluentui/react/lib/Text';
import { Icon, Stack } from '@fluentui/react';
import ApiManager from '@/services/ApiManager';
import 'office-ui-fabric-react/dist/css/fabric.css';
import { withTranslation } from 'react-i18next';
import { Grid } from '@mui/material';
import NotificationManager from '@/services/NotificationManager';
import {
  columnHeader,
  PIMHearderText,
  PIMsearchBoxStyles,
  uiderrorcomp1,
  uiderrorcomp2,
  
} from '../PimStyles';
import { DefaultButton } from 'office-ui-fabric-react';

class UidComparsion extends React.Component<any, any> {
  apiClient = new ApiManager().CreateApiClient();
  stausofRecords = true;

  notify = new NotificationManager();
  private searchTimeout: NodeJS.Timeout | null;
  constructor(props: any) {
    super(props);
    this.state = {
      currentRecord: [],
      currentRecord1: [],
      currentRecord2: [],
      enableEditButton: false,
      enableActiveButton: false,
      inActivePage: false,
      rolevisible: false,
      copyRecord: [],
      searchKey: '',
      selectedRoleIds: [],
      comp1: [],
      comp0: [],
      selectedRowData: [],
      noSelection: false,
      isRecord: false,
      intitalsearch: false,
      uidcompflag: null,
    };
    this.searchTimeout = null;
  }

  async componentDidMount(): Promise<void> {
    this.refresh();
  }

  async refresh() {
    this.setState({ isRecord: true, currentRecord: [] });

    try {
      const apiResponse1 = (await this.apiClient.getTotalCounts())?.result;

      if (apiResponse1 && apiResponse1.length > 0) {
        const mappedResponse = apiResponse1.map((responseItem: any) => ({
          viewcountdata: responseItem.viewColumnDataCount.totalcountvw,
          jsoncountdata: responseItem.jsonFormatDataCount.totalcountjson,
        }));

        this.setState({
          currentRecord2: mappedResponse,
        });
      }

      const flagcheck = this.state.searchKey;
      if (flagcheck !== '') {
        const apiResponse = await this.apiClient.uidColumndataCheck(this.state.searchKey);

        if (apiResponse?.result) {
          const uidcomp = apiResponse.result;

          let uidcompflag = false;

          const columns = Object.keys(uidcomp);
          for (let j = 0; j < columns.length; j++) {
            const columnName: any = columns[j];
            const columnValue = uidcomp[columnName]?.toString();
            if (columnValue && columnValue.includes('Mismatched')) {
              uidcompflag = true;
              break;
            }
          }
          this.setState({ uidcompflag });

          const uidcomp1 = uidcomp?.[0]?.columnData || [];
          const uidcomp2 = uidcomp?.[1] || [];
          this.setState({
            comp0: [uidcomp1],
            comp1: [uidcomp2],
            intitalsearch: true,
          });

          this.setState({
            currentRecord: uidcomp.map((item: any, index: number) => ({
              index: index + 1,
              ...item,
              columnData: item.columnData,
            })),
          });

          const uidcompObject = uidcomp?.[1] ? [{ columnData1: uidcomp[1] }] : [];
          this.setState({
            currentRecord1: uidcompObject.map((item: any, index: number) => ({
              index: index + 1,
              ...item,
              columnData1: item.columnData1,
            })),
          });
        } else {
          this.setState({ currentRecord: [] });
        }
      }
    } catch (error: any) {
      this.notify.showErrorNotify(error.message);
    }
    this.setState({ isRecord: false });
  }
  onSearch = (newValue: string) => {
    if (this.searchTimeout !== null) {
      clearTimeout(this.searchTimeout as NodeJS.Timeout);
    }

    this.searchTimeout = setTimeout(() => {
      this.setState({ searchKey: newValue });
    }, 100);
  };

  render() {
    const { currentRecord, currentRecord1, currentRecord2 } = this.state;
    const filteredData = this.state.currentRecord.length > 0 ? [this.state.currentRecord[0]] : [];

    const { t } = this.props;
    const columns: any = [
      {
        key: 'columndata',
        name: <div className={columnHeader}>{`${t('COL_COLUMN_DATA')}`}</div>,
        selector: (row: any) => {
          return (
            <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid black' }}>
              <tbody>
                {Object.entries(row.columnData || {}).map(([key, value]) => (
                  <tr key={key}>
                    <td style={{ border: '1px solid black', padding: '4px' }}>{key}</td>
                    <td style={{ border: '1px solid black', padding: '4px' }}>{value}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          );
        },
        isVisible: true,
        minWidth: '120px',
      },
      {
        key: 'coldata',
        name: <div className={columnHeader}>{`${t('COL_JSON_FORMATE_DATA')}`}</div>,
        selector: (row: any, index: number) => {
          if (index === 0) {
            return (
              <div>
                {currentRecord1.map((record: any, idx: any) => (
                  <div key={idx}>
                    <table
                      style={{
                        width: '100%',
                        borderCollapse: 'collapse',
                        border: '1px solid black',
                      }}
                    >
                      <tbody>
                        {Object.entries(record.columnData1 || {}).map(([key, value]) => (
                          <tr key={key}>
                            <td style={{ border: '1px solid black', padding: '4px' }}>{key}</td>
                            <td style={{ border: '1px solid black', padding: '4px' }}>{value}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                ))}
              </div>
            );
          }
          return null;
        },
        isVisible: true,
        minWidth: '120px',
      },
    ];

    const Initialcolumn: any = [
      {
        key: 'columndata',
        name: <div className={columnHeader}>{`${t('COL_COLUMN_DATA')}`}</div>,
        selector: (row: any) => (
          <span title={row.viewcountdata}>Total Count :{row.viewcountdata}</span>
        ),
        isVisible: true,
        minWidth: '120px',
      },
      {
        key: 'coldata',
        name: <div className={columnHeader}>{`${t('COL_JSON_FORMATE_DATA')}`}</div>,
        selector: (row: any) => (
          <span title={row.jsoncountdata}>Total Count :{row.jsoncountdata}</span>
        ),
        isVisible: true,
        minWidth: '120px',
      },
    ];

    return (
      <div className="layout width-100">
        <div className="bg-Color">
          <div style={{ marginBottom: '6px' }}>
            <Stack>
              <Text
                className="color-blue text"
                key={'xxLarge'}
                variant={'xxLarge' as ITextProps['variant']}
                styles={PIMHearderText}
                nowrap
                block
              >
                {t('MENU_UID_COMPARSION')}
              </Text>
            </Stack>
          </div>
          <Grid lg={12} item container spacing={1} direction={'row'}>
            <Grid item lg={3} xs={4}>
              <SearchBox
                className="searchBox"
                styles={PIMsearchBoxStyles}
                placeholder={t('SEARCH_PLACEHOLDER')}
                onChange={(e: any) => this.onSearch(e.target.value)}
                value={this.state.searchKey}
                onClear={() => this.setState({ searchKey: '' })}
              />
            </Grid>
            <Grid item lg={6} xs={4}></Grid>
            <div style={{ marginTop: '1.2%', fontSize: '17px', marginLeft: '2%' }}>Status</div>
            <DefaultButton
              style={{ width: 90, marginTop: '0.8%', marginLeft: '1%' }}
              styles={this.state.uidcompflag ? { ...uiderrorcomp1 } : { ...uiderrorcomp2 }}
              text={
                this.state.uidcompflag === null
                  ? ''
                  : this.state.uidcompflag
                  ? t('False')
                  : t('True')
              }
            />
            <Grid item lg={0.1} xs={4}></Grid>

            <Grid item lg={0.5} xs={4}>
              <Icon
                iconName="Search"
                title={this.props.t('BTN_SEARCH')}
                className="iconStyle"
                onClick={() => {
                  if (this.state.searchKey.trim() !== '') {
                    this.refresh();
                    this.setState({ intitalsearch: true });
                  }
                }}
              />
            </Grid>
            <Grid item lg={0.5} xs={4}>
              <Icon
                iconName="Refresh"
                title={this.props.t('BTN_REFRESH')}
                className="iconStyle"
                onClick={() => {
                  this.refresh();
                  this.setState({ intitalsearch: false, searchKey: '', uidcompflag: null });
             
                }}
              />
            </Grid>
          </Grid>

          <div>
          <DataTable
            className='uidscreen'
            columns={Initialcolumn}
            data={currentRecord2}
            // pagination={true}
            // selectableRows={this.state.selectedRowData}
            // onSelectedRowsChange={this.rowSelection}
            // clearSelectedRows={this.state.noSelection}
            // selectableRowsHighlight
            highlightOnHover
            responsive
            fixedHeader
            striped
            fixedHeaderScrollHeight="67vh"
            // paginationComponentOptions={{ rowsPerPageText: `${this.props.t('ROWS_PER_PAGE')}` }}
            // paginationPerPage={this.Defaultperpage()}
            // paginationRowsPerPageOptions={this.perpage()}
            noDataComponent={
              <div className="noDataWidth">
                {/* {<DataTable columns={""} data={[{ '': '' }]} />} */}
                <Stack className="noRecordsWidth">
                  {this.state.isRecord === true
                    ? `${this.props.t('RECORDS')}`
                    : `${this.props.t('NO_RECORDS')}`}
                </Stack>
              </div>
            }
          />
          </div>
          <div className="uidcompdata">
            {this.state.intitalsearch === true && (
              <div>
              <DataTable
                className='uidscreen'  
                columns={columns}
                data={filteredData}
                // pagination={true}
                // selectableRows={this.state.selectedRowData}
                // onSelectedRowsChange={this.rowSelection}
                // clearSelectedRows={this.state.noSelection}
                // selectableRowsHighlight
                highlightOnHover
                responsive
                fixedHeader
                striped
                fixedHeaderScrollHeight="57.7vh"
                noTableHead={this.state.intitalsearch}
                // paginationComponentOptions={{ rowsPerPageText: `${this.props.t('ROWS_PER_PAGE')}` }}
                // paginationPerPage={this.Defaultperpage()}
                // paginationRowsPerPageOptions={this.perpage()}
                noDataComponent={
                  <div className="noDataWidth">
                    {/* {<DataTable columns={columns} data={[{ '': '' }]} />} */}
                    <Stack className="noRecordsWidth">
                      {this.state.isRecord === true
                        ? `${this.props.t('RECORDS')}`
                        : `${this.props.t('NO_RECORDS')}`}
                    </Stack>
                  </div>
                }
              />
              </div>
            )}
          </div>
        </div>
      </div>
    );
  }
}
const ComponentTranslated: any = withTranslation()(UidComparsion);

function App() {
  return (
    <Suspense fallback="">
      <ComponentTranslated />
    </Suspense>
  );
}

export default App;
