import { DefaultButton, DocumentCard, DocumentCardTitle, Icon, IconButton } from '@fluentui/react';
import Grid from '@mui/material/Grid';
import { t } from 'i18next';
import React, { useState } from 'react';
import {
  PIMcontentStyles,
  PIMfileUpload,
  PIMfooterButtonCancel,
  PIMfooterButtonConfirm,
  PIMRemoveButton,
  removeIcon,
} from '../PimStyles';

const PsUploadImageFolder = (props: any) => {
  const fileInputRef: any = React.useRef(null);
  const [files, setFiles] = useState<Array<File>>([]);
  const [images, setImages] = useState<Array<File>>([]);
  const [error, setError] = useState('');
  const [dragging, setDragging] = useState(false);

  const uploadHandler = () => {
    try {
      const fileInput = document.createElement('input');
      fileInput.type = 'file';
      fileInput.multiple = true;
      fileInput.webkitdirectory = true;
      fileInput.click();
      fileInput.addEventListener('change', (event: any) => {
        const selectedFiles: any = Array.from(event.target.files);
        setImages(selectedFiles);
      });
    } catch (err: any) {
      console.log(err);
    }
  };

  const uploadFiles = () => {
    try {
      setError('');
      const filteredFiles = filterFilesByType(images);

      if (filteredFiles.length === 0) {
        return setError('No valid image files found in the folder');
      }

      // Upload images
      props.onUploadImage(filteredFiles);
      props.ClosePopup();
    } catch (err) {
      console.log(err);
    }
  };

  const filterFilesByType = (fileList: any) => {
    return fileList.filter((file: any) => {
      const fileName = file.name.toLowerCase();
      return fileName.endsWith('.jpg') || fileName.endsWith('.jpeg') || fileName.endsWith('.png');
    });
  };

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.files) {
      const selectedFiles = Array.from(event.target.files);
      setImages(selectedFiles);
    }
  };

  const handleInputChange = (event: Event) => {
    if (event instanceof Event) {
      const inputElement = event.target as HTMLInputElement;
      if (inputElement.files) {
        const selectedFiles = Array.from(inputElement.files);
        setImages(selectedFiles);
      }
    }
  };

  const handleRemoveFile = (i: number) => {
    const updatedImages = [...images];
    updatedImages.splice(i, 1);
    setImages(updatedImages);
    const fileInput = document.querySelector('.customInputImg');
    if (fileInput && fileInput instanceof HTMLInputElement) {
      fileInput.value = '';
      const newFileInput = document.createElement('input');
      newFileInput.type = 'file';
      newFileInput.accept = '.jpg, .jpeg, .png';
      newFileInput.multiple = true;
      newFileInput.classList.add('customInputImg');
      newFileInput.addEventListener('change', handleInputChange);
      if (fileInput.parentNode) {
        fileInput.parentNode.replaceChild(newFileInput, fileInput);
      }
    }
  };

  const removeFile = (filename: any) => {
    setFiles(images.filter((file: any) => file.name !== filename));
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

  const handleDrop = (event: any) => {
    try {
      setDragging(false);
      event.preventDefault();
      event.stopPropagation();
      const selectedFiles: any = Array.from(event.dataTransfer.files);
      setImages(selectedFiles);
    } catch (err: any) {
      console.log(err);
    }
  };

  function UploadFile() {
    if (images.length == 0) {
      uploadHandler();
    } else {
      console.log('Cannot add more than one file');
    }
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
                    minWidth: '60px',
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
              >
                <label className="UploadButton" style={{ fontSize: '20px' }}>
                  {t('DRAG_AND_DROP_PICTUTE')}
                </label>
                <input
                  className="customInputImg"
                  type="file"
                  accept=".jpg, .jpeg, .png"
                  multiple
                  onChange={handleFileChange}
                />
                <div className={PIMfileUpload.fileList}>
                  {images &&
                    images.map((f: any, i: number) => {
                      const imageUrl = URL.createObjectURL(f);
                      return (
                        <div className={PIMfileUpload.listItem} key={f.name}>
                          <img src={imageUrl} alt={f.name} className={PIMfileUpload.img} />
                          <p className={PIMfileUpload.p}>{f.name}</p>
                          <p className={PIMfileUpload.p}>
                            {f.size < 100000
                              ? `File Size: ${(parseInt(f.size) / 1000).toFixed(2)} KB`
                              : `File Size: ${(parseInt(f.size) / 1e6).toFixed(2)} MB`}
                          </p>
                          <IconButton
                            styles={PIMRemoveButton}
                            iconProps={removeIcon}
                            onClick={() => {
                              handleRemoveFile(i);
                            }}
                          />
                        </div>
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
        <div className={PIMcontentStyles.Excelfooter}>
          <Grid lg={12} item container spacing={2} direction={'row'}>
            <Grid item lg={1} xs={12} />
            <Grid item lg={4} xs={12}>
              <DefaultButton
                className="button"
                styles={PIMfooterButtonConfirm}
                disabled={images.length === 0}
                onClick={() => uploadFiles()}
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
    </div>
  );
};

export default PsUploadImageFolder;
