import NotificationManager from '@/services/NotificationManager';
import { ISelectableOption, TextField } from '@fluentui/react';
import { t } from 'i18next';
import React, { forwardRef, useEffect, useImperativeHandle, useState } from 'react';
import { ComboBox, DatePicker, Dropdown, IColumn, Modal, Stack } from '@fluentui/react';
import { DefaultButton, IconButton } from '@fluentui/react/lib/Button';
import { colors, Grid } from '@mui/material';
import axios, { AxiosResponse } from 'axios';

import { cancelIcon, iconButtonStyles, Remove } from '@/common/properties';
import {
  PIMcontentStyles,
  PIMfooterButtonCancel,
  PIMfooterButtonConfirm,
  HeadInfo,
} from './PimStyles';
import { FaBlackTie } from 'react-icons/fa';

const notify = new NotificationManager();
export const renderOption = (option?: ISelectableOption<any>): JSX.Element | null => {
  if (!option) {
    return null;
  }
  return <span title={option.text}>{option.text}</span>;
};

export const downloadFileCommon = (url: any) => {
  const link = document.createElement('a');
  link.href = url;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  notify.HideNotify(t('DOWNLOAD_STARTED'));
};
function getTimestampString(separator: string): string {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  const hours = String(now.getHours()).padStart(2, '0');
  const minutes = String(now.getMinutes()).padStart(2, '0');
  const seconds = String(now.getSeconds()).padStart(2, '0');
  const milliseconds = String(now.getMilliseconds()).padStart(3, '0');

  return `${year}${separator}${month}${separator}${day}${separator}${hours}${separator}${minutes}${separator}${seconds}${separator}${milliseconds}`;
}
function randomString(length: number): string {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
  let result = '';
  for (let i = 0; i < length; i++) {
    result += chars.charAt(Math.floor(Math.random() * chars.length));
  }
  return result;
}
function toUniqueFileNameUrl(fileName: string): string {
  const pattern = /[^a-zA-Z0-9]/;

  if (!fileName) {
    return '';
  }

  const res = fileName.split('.');

  return (
    res[0].replace(pattern, '_') +
    '_' +
    randomString(3) +
    '_' +
    getTimestampString('_') +
    '.' +
    res[res.length - 1]
  );
}
export const downloadImageUID = (url: any, Filename: any) => {
  notify.ImageHideNotify(t('DOWNLOAD_STARTED'));
  document.body.classList.add('loading-indicator');
  axios({
    url: url,
    method: 'get',
    responseType: 'blob',
  })
    .then((response: any) => {
      notify.showSuccessNotify(t('DOWNLOAD_COMPLETED'));
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', Filename);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      document.body.classList.remove('loading-indicator');
    })
    .catch((error: any) => console.error('Error downloading file:', error));
};
// export const downloadFileUID = (url: any, fileName: any) => {
//   notify.ImageHideNotify(t('DOWNLOAD_STARTED'));
//   document.body.classList.add('loading-indicator');
//   axios({
//     url: url,
//     method: 'get',
//     responseType: 'blob',
//   })
//     .then((response: any) => {
//       notify.showSuccessNotify(t('DOWNLOAD_COMPLETED'));
//       const url = window.URL.createObjectURL(new Blob([response.data]));
//       const link = document.createElement('a');
//       const uniqueFileName = fileName;
//       const finalFileName = uniqueFileName;
//       link.href = url;
//       link.setAttribute('download', finalFileName);
//       document.body.appendChild(link);
//       link.click();
//       document.body.removeChild(link);
//       document.body.classList.remove('loading-indicator');
//     })
//     .catch((error: any) => console.error('Error downloading file:', error));
// };

export const downloadFileUID = (url: any, fileName: any, exportModel: any) => {
  notify.ImageHideNotify(t('DOWNLOAD_STARTED'));
  document.body.classList.add('loading-indicator');

  axios({
      url: url,
      method: 'post',
      data: exportModel,
      responseType: 'blob',
  })
  .then((response: any) => {
      notify.showSuccessNotify(t('DOWNLOAD_COMPLETED'));
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      document.body.classList.remove('loading-indicator');
  })
  .catch((error: any) => {
      console.error('Error downloading file:', error);
      notify.showErrorNotify(t('DOWNLOAD_FAILED')); // Optional error notification
  });
};

export const downloadFileMarket = (url: any) => {
  notify.ImageHideNotify(t('DOWNLOAD_STARTED'));
  document.body.classList.add('loading-indicator');
  axios({
    url: url,
    method: 'get',
    responseType: 'blob',
  })
    .then((response: any) => {
      notify.showSuccessNotify(t('DOWNLOAD_COMPLETED'));
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      const fileName = 'MarketPlace.xlsx';
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      document.body.classList.remove('loading-indicator');
    })
    .catch((error: any) => console.error('Error downloading file:', error));
};

export const downloadFileItemCodeAll = (url: any) => {
  notify.ImageHideNotify(t('DOWNLOAD_STARTED'));
  document.body.classList.add('loading-indicator');
  axios({
    url: url,
    method: 'get',
    responseType: 'blob',
  })
    .then((response: any) => {
      notify.showSuccessNotify(t('DOWNLOAD_COMPLETED'));
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      const fileName = 'Itemcode Current View.xlsx';
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      document.body.classList.remove('loading-indicator');
    })
    .catch((error: any) => console.error('Error downloading file:', error));
};

export const downloadFileCommontemplate = (url: any, tempName: any) => {
  notify.ImageHideNotify(t('DOWNLOAD_STARTED'));
  document.body.classList.add('loading-indicator');
  axios({
    url: url,
    method: 'get',
    responseType: 'blob',
  })
    .then((response: any) => {
      notify.showSuccessNotify(t('DOWNLOAD_COMPLETED'));
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      const fileName = `${tempName}.xlsx`;
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      document.body.classList.remove('loading-indicator');
    })
    .catch((error: any) => console.error('Error downloading file:', error));
};
export const downloadFileItemCodeReady = (url: any) => {
  notify.ImageHideNotify(t('DOWNLOAD_STARTED'));
  document.body.classList.add('loading-indicator');
  axios({
    url: url,
    method: 'get',
    responseType: 'blob',
  })
    .then((response: any) => {
      notify.showSuccessNotify(t('DOWNLOAD_COMPLETED'));
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      const fileName = 'Itemcode Data.xlsx';
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      document.body.classList.remove('loading-indicator');
    })
    .catch((error: any) => console.error('Error downloading file:', error));
};

export const downloadFileMarster = (url: any, Fname: any) => {
  notify.ImageHideNotify(t('DOWNLOAD_STARTED'));
  document.body.classList.add('loading-indicator');
  axios({
    url: url,
    method: 'get',
    responseType: 'blob',
  })
    .then((response: any) => {
      notify.showSuccessNotify(t('DOWNLOAD_COMPLETED'));
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      const fileName = `${Fname}.xlsx`;
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      document.body.classList.remove('loading-indicator');
    })
    .catch((error: any) => console.error('Error downloading file:', error));
};

export const downloadFileItemcode = (url: any) => {
  notify.ImageHideNotify(t('DOWNLOAD_STARTED'));
  document.body.classList.add('loading-indicator');
  axios({
    url: url,
    method: 'get',
    responseType: 'blob',
  })
    .then((response: any) => {
      notify.showSuccessNotify(t('DOWNLOAD_COMPLETED'));
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      const fileName = 'CAM Current View.xlsx';
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      document.body.classList.remove('loading-indicator');
    })
    .catch((error: any) => console.error('Error downloading file:', error));
};

export const downloadFileItemcodeSearch = (url: any) => {
  notify.ImageHideNotify(t('DOWNLOAD_STARTED'));
  document.body.classList.add('loading-indicator');
  axios({
    url: url,
    method: 'get',
    responseType: 'blob',
  })
    .then((response: any) => {
      notify.showSuccessNotify(t('DOWNLOAD_COMPLETED'));
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      const fileName = 'CAM Search.xlsx';
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      document.body.classList.remove('loading-indicator');
    })
    .catch((error: any) => console.error('Error downloading file:', error));
};

export const downloadFileItemcodeAll = (url: any) => {
  notify.ImageHideNotify(t('DOWNLOAD_STARTED'));
  document.body.classList.add('loading-indicator');
  axios({
    url: url,
    method: 'get',
    responseType: 'blob',
  })
    .then((response: any) => {
      notify.showSuccessNotify(t('DOWNLOAD_COMPLETED'));
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      const fileName = 'CAM Current View.xlsx';
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      document.body.classList.remove('loading-indicator');
    })
    .catch((error: any) => console.error('Error downloading file:', error));
};

export const downloadFileItemcodeReady = (url: any, filename: any) => {
  notify.ImageHideNotify(t('DOWNLOAD_STARTED'));
  document.body.classList.add('loading-indicator');
  axios({
    url: url,
    method: 'get',
    responseType: 'blob',
  })
    .then((response: any) => {
      notify.showSuccessNotify(t('DOWNLOAD_COMPLETED'));
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      const fileName = `CAM Data ${filename}.xlsx`;
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      document.body.classList.remove('loading-indicator');
    })
    .catch((error: any) => console.error('Error downloading file:', error));
};
export const downloadStudio = (url: any, filename: any) => {
  notify.ImageHideNotify(t('DOWNLOAD_STARTED'));
  document.body.classList.add('loading-indicator');
  axios({
    url: url,
    method: 'get',
    responseType: 'blob',
  })
    .then((response: any) => {
      notify.showSuccessNotify(t('DOWNLOAD_COMPLETED'));
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      const fileName = `${filename}.xlsx`;
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      document.body.classList.remove('loading-indicator');
    })
    .catch((error: any) => console.error('Error downloading file:', error));
};

export const downloadFileMarketPlace = (url: any) => {
  notify.ImageHideNotify(t('DOWNLOAD_STARTED'));
  document.body.classList.add('loading-indicator');
  axios({
    url: url,
    method: 'get',
    responseType: 'blob',
  })
    .then((response: any) => {
      notify.showSuccessNotify(t('DOWNLOAD_COMPLETED'));
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      const fileName = 'MarketPlace.xlsx';
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      document.body.classList.remove('loading-indicator');
    })
    .catch((error: any) => console.error('Error downloading file:', error));
};

export const downloadFileItemCodeSearch = (url: any) => {
  notify.ImageHideNotify(t('DOWNLOAD_STARTED'));
  document.body.classList.add('loading-indicator');
  axios({
    url: url,
    method: 'get',
    responseType: 'blob',
  })
    .then((response: any) => {
      notify.showSuccessNotify(t('DOWNLOAD_COMPLETED'));
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      const fileName = 'CAM.xlsx';
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      document.body.classList.remove('loading-indicator');
    })
    .catch((error: any) => console.error('Error downloading file:', error));
};

export const downloadFileDataRequest = (url: any, dataRequestId: any) => {
  notify.ImageHideNotify(t('DOWNLOAD_STARTED'));
  document.body.classList.add('loading-indicator');
  axios({
    url: url,
    method: 'get',
    responseType: 'blob',
  })
    .then((response: any) => {
      notify.showSuccessNotify(t('DOWNLOAD_COMPLETED'));
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      const uniqueFileName = `DataRequest-${dataRequestId}.xlsx`;
      const fileName = toUniqueFileNameUrl(uniqueFileName);
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      document.body.classList.remove('loading-indicator');
    })
    .catch((error: any) => console.error('Error downloading file:', error));
};

export const downloadFileMarketplace = (url: any) => {
  notify.ImageHideNotify(t('DOWNLOAD_STARTED'));
  document.body.classList.add('loading-indicator');
  axios({
    url: url,
    method: 'get',
    responseType: 'blob',
  })
    .then((response: any) => {
      notify.showSuccessNotify(t('DOWNLOAD_COMPLETED'));
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      const uniqueFileName = 'Marketplace-Attribute.xlsx';
      const fileName = toUniqueFileNameUrl(uniqueFileName);
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      document.body.classList.remove('loading-indicator');
    })
    .catch((error: any) => console.error('Error downloading file:', error));
};

export interface CommonDateProps {
  onMinDateChange: (date: Date | undefined) => void;
  onToDateChange: (date: Date | undefined) => void;
  label?: string;
}

export interface CommonDateRef {
  resetDates: () => void;
}

const CommonDate: React.ForwardRefRenderFunction<CommonDateRef, CommonDateProps> = (
  { onMinDateChange, onToDateChange, label },
  ref,
) => {
  const [startDate, setStartDate] = useState<any>(undefined);

  const [toDate, setToDate] = useState<any>(undefined);

  const [datePickerVisible, setDatePickerVisible] = useState(false);

  const onSubmit = () => {
    onMinDateChange(startDate);
    onToDateChange(toDate);
  };

  useImperativeHandle(ref, () => ({
    resetDates,
  }));

  const resetDates = () => {
    setStartDate(undefined);
    setToDate(undefined);
  };

  return (
    <>
      <TextField
        placeholder={
          startDate || toDate
            ? startDate
              ? toDate
                ? `${startDate?.toLocaleDateString('en-GB')} - ${toDate?.toLocaleDateString(
                  'en-GB',
                )}`
                : `${startDate?.toLocaleDateString('en-GB')} - ${t('PLACEHOLDER_TO_DATE')}`
              : toDate
                ? `${t('PLACEHOLDER_FROM_DATE')} -${toDate?.toLocaleDateString('en-GB')}`
                : ''
            : `${t('PLACEHOLDER_FROM_DATE')} - ${t('PLACEHOLDER_TO_DATE')}`
        }
        iconProps={{ iconName: 'Calendar' }}
        onClick={() => setDatePickerVisible(true)}
        readOnly
      />

      <Modal
        isOpen={datePickerVisible}
        containerClassName={PIMcontentStyles.confirmContainer}
        onDismiss={() => {
          setDatePickerVisible(false),
            setStartDate(undefined),
            setToDate(undefined),
            onMinDateChange(undefined),
            onToDateChange(undefined);
        }}
        isBlocking={false}
      >
        <div className={PIMcontentStyles.header}>
          {/* <Grid
            lg={12}
            item
            container
            spacing={2}
            direction={'row'}
            borderBottom={1}
            borderColor={'#d9d6d3'}
          > */}
          {/* <Grid item lg={10} xs={10}> */}
          <span className="daterange">{`${label ?? t('DATE_RANGE')}`}</span>
          {/* </Grid> */}
          {/* <Grid item lg={2} xs={1}> */}
          <IconButton
            // styles={iconButtonStyles}
            styles={{ root: { position: 'absolute', top: 0, right: 0, color: "black" } }}
            iconProps={cancelIcon}
            ariaLabel="Close popup modal"
            onClick={() => {
              setDatePickerVisible(false), setStartDate(undefined),
                setToDate(undefined), onMinDateChange(undefined),
                onToDateChange(undefined)
            }}
          />
          {/* </Grid> */}
          {/* </Grid> */}
        </div>
        <div className={PIMcontentStyles.body}>
          {/* <Grid lg={12} item container spacing={2} direction={'row'} borderBottom={1}
            borderColor={'#d9d6d3'} marginBottom={2}> */}
          <Grid lg={12} item container spacing={2} direction={'row'}>
            <Grid item lg={0.8} xs={12} />
            <Grid item lg={5.3} xs={5}>
              <DatePicker
                className="datePicker"
                label={`${t('PLACEHOLDER_FROM_DATE')}`}
                placeholder={`${t('PLACEHOLDER_DATE_FORMAT')}`}
                showMonthPickerAsOverlay={true}
                formatDate={(e: any) => e.toLocaleDateString('en-GB')}
                maxDate={toDate != undefined ? new Date(toDate) : new Date()}
                onSelectDate={(date: any) => {
                  setStartDate(date);
                }}
                value={startDate}
                showGoToToday={true}
              />
            </Grid>
            <Grid item lg={5.3} xs={5}>
              <DatePicker
                className="datePicker"
                label={`${t('PLACEHOLDER_TO_DATE')}`}
                placeholder={`${t('PLACEHOLDER_DATE_FORMAT')}`}
                showMonthPickerAsOverlay={true}
                formatDate={(e: any) => e.toLocaleDateString('en-GB')}
                minDate={startDate != undefined ? new Date(startDate) : undefined}
                maxDate={new Date()}
                onSelectDate={(date: any) => {
                  setToDate(date);
                }}
                value={toDate}
                showGoToToday={true}
              />
            </Grid>
            <Grid item lg={0.7} xs={12} />
          </Grid>
        </div>
        <div className={PIMcontentStyles.footers}>
          <Grid lg={12} item container spacing={4} direction={'row'}>
            <Grid item lg={2} xs={12} />
            <Grid item lg={4} xs={12}>
              <DefaultButton
                type="submit"
                className="button"
                text={`${t('ADD_BTN_SUBMIT')}`}
                styles={PIMfooterButtonConfirm}
                disabled={!startDate && !toDate}
                onClick={() => {
                  onSubmit();
                  setDatePickerVisible(false);
                }}
              />
            </Grid>
            <Grid item lg={4} xs={12}>
              <DefaultButton
                text={`${t('BTN_CANCEL')}`}
                className="button"
                styles={PIMfooterButtonCancel}
                onClick={() => {
                  setDatePickerVisible(false);
                  setToDate(undefined);
                  setStartDate(undefined);
                  onMinDateChange(undefined);
                  onToDateChange(undefined);
                }}
              />
            </Grid>
            <Grid item lg={2} xs={12} />
          </Grid>
        </div>
      </Modal>
    </>
  );
};

export default forwardRef<CommonDateRef, CommonDateProps>(CommonDate);
