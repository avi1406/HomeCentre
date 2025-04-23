import { DefaultButton, DocumentCard, DocumentCardTitle, IconButton, Modal } from '@fluentui/react';
import Grid from '@mui/material/Grid';
import { t } from 'i18next';
import React, { useState } from 'react';
import {
  PIMfileUpload,
  PIMRemoveButton,
  removeIcon,
  PIMcontentStyles,
  PIMfooterButtonConfirm,
  PIMfooterButtonCancel,
  PIMButtons,
} from '../PimStyles';

const UploadData = (props: any) => {
  const fileInputRef: any = React.useRef(null);
  const [files, setFiles] = useState(Array<File>());
  const [images, setImages] = useState(Array<File>());
  const [error, setError] = useState('');
  const [dragging, setDragging] = useState(false);
  const [visible, setVisible] = useState(false);

  const uploadHandler = () => {
    try {
      const fileInput = document.createElement('input');
      fileInput.type = 'file';
      fileInput?.click();
      fileInput.addEventListener('change', (event: any) => {
        uploadFiles(event.target?.files[0]);
      });
    } catch (err: any) {
      console.log(err);
    }
  };
  const uploadFiles = (event?: any) => {
    try {
      setError('');
      const file = event;
      for (var i = 0; i < files.length; i++) {
        if (file?.name == files[i]?.name) {
          return setError('File Already Exists');
        }
      }
      const fileType = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';
      const fileSize = 10000000;
      if (file?.type == fileType && file?.size <= fileSize && files?.length < 1) {
        setFiles([...files, file]);
        const formData = new FormData();
        formData.append(file.name, file, file.name);
      } else {
        (file?.type || file?.size) == undefined
          ? setError('')
          : file?.type != fileType
          ? setError('Upload Excel files only')
          : file?.size > fileSize
          ? setError('Max File size is 10MB')
          : files?.length <= 1
          ? setError('Cannot add more than one file')
          : setError('Something went wrong');
      }
    } catch (err: any) {
      console.log(err);
    }
  };

  const removeFile = (filename: any) => {
    setFiles(files.filter((file: any) => file.name !== filename));
    setError('');
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };
  function handleDragEnter(event: any) {
    event.preventDefault();
    setDragging(true);
  }
  function handleDragOver(event: any) {
    event.preventDefault();
    setDragging(true);
  }
  function handleDragLeave(event: any) {
    event.preventDefault();
    setDragging(false);
  }
  function handleDrop(event: any) {
    try {
      setDragging(false);
      event.preventDefault();
      event.stopPropagation();
      uploadFiles(event.dataTransfer.files[0]);
    } catch (err: any) {
      console.log(err);
    }
  }
  function UploadFile() {
    if (files.length == 0) {
      uploadHandler();
    } else {
      console.log('Cannot add more than one file');
    }
  }
  function SendData(excelFile?: any, images?: any) {
    props.UploadData(excelFile, images);
  }
  return (
    <div>
      <div className={PIMfileUpload.fileCard} onDrop={uploadHandler}>
        <Grid lg={12} item container spacing={2} direction={'row'}>
          <Grid item lg={12} xs={12}>
            <div>
              <DocumentCard
                styles={{
                  root: {
                    minWidth: '150px',
                    maxWidth: 'none',
                    width: '100%',
                    height: '70%',
                    border: `2px dashed ${dragging ? 'blue' : 'gray'}`,
                    display: 'flex',
                    flexDirection: 'column',
                    justifyContent: 'center',
                    alignItems: 'center',
                    textAlign: 'center',
                    cursor: 'pointer',
                  },
                }}
                onDragEnter={handleDragEnter}
                onDragOver={handleDragOver}
                onDragLeave={handleDragLeave}
                onDrop={handleDrop}
                onClick={UploadFile}
              >
                <DocumentCardTitle
                  className="UploadButton"
                  styles={{ root: { minWidth: '200px', minHeight: '200px' } }}
                  title={t('DRAG_AND_DROP')}
                />
                <div className={PIMfileUpload.fileList}>
                  {files &&
                    files.map((f: any, i: number) => {
                      return (
                        <>
                          <div className={PIMfileUpload.listItem} key={f.name}>
                            <p className={PIMfileUpload.p}>{f.name}</p>
                            <p className={PIMfileUpload.p}>
                              {f.size < 100000
                                ? `File Size: ${(parseInt(f.size) / 1000).toFixed(2)} KB`
                                : `File Size: ${(parseInt(f.size) / 1e6).toFixed(2)} MB`}
                            </p>
                            <IconButton
                              styles={PIMRemoveButton}
                              iconProps={removeIcon}
                              onClick={() => removeFile(f.name)}
                            />
                          </div>
                        </>
                      );
                    })}
                </div>
              </DocumentCard>
            </div>
            <p style={{ color: 'red', textAlign: 'center' }}>{error}</p>
            <br />
          </Grid>
        </Grid>
        <br />
      </div>
      <div className={PIMcontentStyles.Excelfooter}>
        <Grid lg={12} item container spacing={2} direction={'row'}>
          <Grid item lg={1.5} xs={12} />
          <Grid item lg={4} xs={12}>
            <DefaultButton
              className="button"
              styles={PIMfooterButtonConfirm}
              disabled={files.length == 0}
              onClick={() => SendData(files, images)}
              type="submit"
              text={`${t('BTN_CONFIRM')}`}
            />
            <Grid item lg={1.5} xs={12} />
          </Grid>
          <Grid item lg={0.5} xs={12} />
          <Grid item lg={4} xs={12}>
            <DefaultButton
              className="button"
              styles={PIMfooterButtonCancel}
              text={`${t('BTN_CANCEL')}`}
              onClick={() => props.ClosePopup()}
            />
          </Grid>
        </Grid>
      </div>
    </div>
  );
};

export default UploadData;
