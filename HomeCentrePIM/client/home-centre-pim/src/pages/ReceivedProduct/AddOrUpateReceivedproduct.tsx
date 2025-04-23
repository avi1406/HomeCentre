import { ProductAttribute, ProductImage, Studio } from '@/services/ApiClient';
import ApiManager from '@/services/ApiManager';
import NotificationManager from '@/services/NotificationManager';
import { Checkbox, DatePicker, Dropdown, Icon, TextField } from '@fluentui/react';
import { colors, Grid } from '@mui/material';
import { t } from 'i18next';
import { Text } from '@fluentui/react/lib/Text';
import { DefaultButton } from 'office-ui-fabric-react';
import React, { Component } from 'react';
import ReactFormValidation from 'react-form-input-validation';
import {
  PIMcontentStyles,
  PIMdropdownStyles,
  PIMfooterButtonCancel,
  PIMfooterButtonCancel1,
  PIMfooterButtonConfirm,
  activeText1,
} from '../PimStyles';
import { red } from '@mui/material/colors';

export default class AddOrUpdateReceivedProducts extends Component<any, any> {
  form: ReactFormValidation;
  apiClient = new ApiManager().CreateApiClient();
  notify = new NotificationManager();
  typeoption: any;
  Error: any;
  Errormessage = false;
  constructor(props: any) {
    super(props);
    this.state = {
      fields: [
        {
          id: this.props.fields?.length === 1 ? this.props.fields[0].id : 0,
          id1: this.props.fields?.length === 1 ? this.props.fields[0].id : 0,
          itemCode: this.props.fields?.length === 1 ? this.props.fields[0].itemCode : '',
          imageUrl: this.props.fields?.length === 1 ? this.props.fields[0].imageUrl : null,
          imageType: this.props.fields?.length === 1 ? this.props.fields[0].imageType : null,
          receivedDate: this.props.fields?.length === 1 ? this.props.fields[0]?.receivedDate : null,
          qcaction: this.props.fields?.length === 1 ? this.props.fields[0]?.qcaction : '',
        },
      ],
      fields1: [
        {
          id: this.props.fields?.length === 1 ? this.props.fields[0].id : 0,
          id1: this.props.fields?.length === 1 ? this.props.fields[0].id : 0,
          itemCode: this.props.fields?.length === 1 ? this.props.fields[0].itemCode : '',
          imageUrl: this.props.fields?.length === 1 ? this.props.fields[0].imageUrl : null,
          imageType: this.props.fields?.length === 1 ? this.props.fields[0].imageType : null,
          receivedDate: this.props.fields?.length === 1 ? this.props.fields[0]?.receivedDate : null,
          qcaction: this.props.fields?.length === 1 ? this.props.fields[0]?.qcaction : '',
        },
      ],
      itemcodeSting: false,
      selectedItems: [],
      itemcode: this.props.fields[0]?.itemCode || '',
      itemcodeRemove: false,
      errorMessage: null,
      imageData: this.props.fields[0]?.imageUrl || '',
      toDate:
        this.props.fields?.length === 1 ? new Date(this.props.fields[0]?.receivedDate) : new Date(),
      toDate1:
        this.props.fields?.length === 1 ? new Date(this.props.fields[0]?.receivedDate) : null,
      errors: {},
      typeoption: [],
      checkInput: false,
      dropdownval: false,
      dateval: false,
      dropdown: this.props.fields?.length === 1 ? this.props.fields[0].imageType : null,
      divitionValue: this.props.division || null,
      SubmitedStatus: this.props.fields?.length === 1 ? this.props.fields[0].qcaction : '',
      newitemcode: null,
    };
    this.form = new ReactFormValidation(this);
    this.form.useRules({
      itemCode: 'required',
    });
  }

  componentDidMount() {
    this.dropdown();
  }

  async dropdown() {
    var ProductImageType = (await this.apiClient.getProductImageType())?.result;
    var ProductImageoption: { key: number | undefined; text: string | undefined }[] = [];
    var ProductImageoption1: { key: number | undefined; text: string | undefined }[] = [];

    ProductImageType?.forEach((element) => {
      if (this.props.fields?.[0]?.imageType === element.name) {
        ProductImageoption.push({ key: element.id, text: element.name });
      }
      if (element.name !== 'All') {
        ProductImageoption1.push({ key: element.id, text: element.name });
      }
    });
    this.setState({ dropdown: ProductImageoption, dropdown1: ProductImageoption1 });
  }
  updateFieldsWithIsActive = () => {
    const updatedFields = this.state.fields.map((item: any) => ({
      ...item,
      isActive: true,
      userdivision: this.state.divitionValue.join(','),
    }));
    this.setState({
      fields: updatedFields,
    });
  };

  Additem = () => {
    this.updateFieldsWithIsActive();
    const updatedFields = this.state.fields.map((item: any) => ({
      ...item,
      itemCode: parseInt(item.itemCode.trim(), 10),
      userdivision: this.state.divitionValue.join(','),
    }));
    const itemCodes = updatedFields.map((item: any) => item.itemCode);
    const hasDuplicates = itemCodes.some(
      (itemCode: any, index: any) => itemCodes.indexOf(itemCode) !== index,
    );

    if (hasDuplicates) {
      this.setState({ errorMessage: 'Duplicate item codes found' }, () => {
        setTimeout(() => {
          this.setState({ errorMessage: null });
        }, 3000);
      });
      return;
    }
    const isValidItemCode = this.state.fields.every((item: any) => {
      const itemCode = item.itemCode.toString().trim();
      return /^[0-9]+$/.test(itemCode);
    });
    if (!isValidItemCode) {
      this.setState({ itemcodeSting: true });
      setTimeout(() => {
        this.setState({ itemcodeSting: false });
      }, 3000);
      return;
    }
    const body = Array.isArray(updatedFields) ? updatedFields : [updatedFields];
    this.apiClient.addOrUpdateProductImages(body).then((response: any) => {
      let message = `${t('MSG_ADD')}`;
      if (response.hasError) {
        this.Error = response.message;
        this.setState({ errorMessage: this.Error }, () => {
          setTimeout(() => {
            this.setState({ errorMessage: null });
          }, 3000);
        });
      } else {
        this.notify.showSuccessNotify(message);
        this.props.ClosePopup();
        this.props.dataRefresh();
      }
    });
  };

  Callme = () => {
    if (this.state.toDate !== undefined) {
      const timestampToDate = (date: any) => (date ? date.getTime() : null);
      if (timestampToDate(this.state.toDate1) !== timestampToDate(this.state.toDate)) {
        this.setState({ dateval: true });
      } else {
        this.setState({ dateval: false });
      }
    } else {
      this.setState({ dropdownval: false });
    }

    if (this.state.typeopt !== undefined && this.props.fields?.[0] !== undefined) {
      if (this.state.typeopt !== this.props.fields[0].imageType) {
        this.setState({ dropdownval: true });
      } else {
        this.setState({ dropdownval: false });
      }
    }
  };

  updateitem = () => {
    if (this.state.fields1.length != 0) {
      this.state.fields.forEach((item: any) => {
        let { id, itemCode, imageType, imageUrl, receivedDate } = item;
        var url = imageUrl ? imageUrl : undefined;
        const itemcode = parseInt(itemCode, 10);
        const oldItemcode = parseInt(this.state.fields1[0].itemCode, 10);
        if (oldItemcode == itemcode) {
          itemCode = undefined;
        }
        var date = receivedDate || undefined;
        const userdivision = this.state.divitionValue.join(',');
        this.apiClient
          .updateReceivedproducts(id, itemCode, imageType, url, date, userdivision)
          .then((response: any) => {
            let message = `${t('MSG_UPDATE')}`;
            if (response.hasError) {
              this.Error = response.message;
              this.setState({ errorMessage: this.Error }, () => {
                setTimeout(() => {
                  this.setState({ errorMessage: null });
                }, 3000);
              });
            } else {
              this.notify.showSuccessNotify(message);
              this.props.dataRefresh();
              this.props.ClosePopup();
            }
          });
      });
    }
  };
  addNewRow = () => {
    const newFieldsArray = [...this.state.fields];
    const maxId = Math.max(...newFieldsArray.map((item) => item.id1), 0);
    const newId = maxId + 1;
    newFieldsArray.push({
      id: 0,
      id1: newId,
      itemCode: '',
      imageUrl: null,
      imageType: null,
      receivedDate: null,
      ImagetypeId: null,
    });
    this.setState({
      fields: newFieldsArray,
    });
  };

  handleCheckboxChange = (index: any) => {
    const selectedItems = [...this.state.selectedItems];
    if (selectedItems.includes(index)) {
      const updatedItems = selectedItems.filter((item) => item !== index);
      this.setState({ selectedItems: updatedItems });
    } else {
      selectedItems.push(index);
      this.setState({ selectedItems });
    }
  };
  handleRemoveButtonClick = () => {
    const { fields, selectedItems } = this.state;
    const sortedSelectedItems = [...selectedItems].sort((a, b) => b - a);
    const updatedFields = fields.slice();
    if (sortedSelectedItems.length === updatedFields.length) {
      sortedSelectedItems.forEach((index) => {
        if (index != 0) {
          const itemToRemoveIndex = updatedFields.findIndex((field: any) => field.id1 === index);
          if (itemToRemoveIndex !== -1) {
            updatedFields.splice(itemToRemoveIndex, 1);
          }
        }
      });
    } else {
      sortedSelectedItems.forEach((index) => {
        const itemToRemoveIndex = updatedFields.findIndex((field: any) => field.id1 === index);
        if (itemToRemoveIndex !== -1) {
          updatedFields.splice(itemToRemoveIndex, 1);
        }
      });
    }
    const updatedFieldsWithConsecutiveIds = updatedFields.map((field: any, index: any) => ({
      ...field,
      id1: index,
    }));
    this.setState({ fields: [] }, () => {
      this.setState(
        {
          fields: updatedFieldsWithConsecutiveIds,
          selectedItems: [],
        },
        () => {
          this.setState({ fields: updatedFieldsWithConsecutiveIds });
        },
      );
    });
  };
  handleChangeEvent = (e: any, index: any) => {
    const { name, value } = e.target;
    const updatedFields = [...this.state.fields];
    updatedFields[index] = {
      ...updatedFields[index],
      [name]: value,
      imageType: updatedFields[index].imageType || undefined,
      receivedDate: updatedFields[index].receivedDate || undefined,
      isActive: true,
    };
    this.setState({ newitemcode: value, fields: updatedFields, imageData: '' }, () => {
      const { fields, errors } = this.state;
      const updatedErrors = { ...errors };
      // if (this.props.fields.length != 0) {
      if (
        this.state.dropdown == 'Drape Image' &&
        (fields[index].imageUrl == null || fields[index].imageUrl == '')
      ) {
        updatedErrors[`${index}_imageUrl`] = 'Please enter Image URL.';
      } else {
        updatedErrors[`${index}_imageUrl`] = '';
      }
      // }
      if (JSON.stringify(updatedErrors) !== JSON.stringify(errors)) {
        this.setState({
          errors: updatedErrors,
        });
      }
    });
  };

  handleChangeDropdown = (item: any, index: any) => {
    const updatedFields = [...this.state.fields];
    updatedFields[index] = {
      ...updatedFields[index],
      imageType: item.text,
      imageUrl: updatedFields[index].imageUrl || undefined,
      receivedDate: updatedFields[index].receivedDate || undefined,
      isActive: true,
      ImagetypeId: item.key,
    };
    this.setState(
      {
        fields: updatedFields,
      },
      () => {
        const { fields, errors } = this.state;
        const updatedErrors = { ...errors };
        // if (this.props.fields.length != 0) {
        if (
          item.text == 'Drape Image' &&
          (fields[index].imageUrl == null || fields[index].imageUrl == '')
        ) {
          updatedErrors[`${index}_imageUrl`] = 'Please enter Image URL.';
        } else {
          updatedErrors[`${index}_imageUrl`] = '';
        }
        // }
        if (JSON.stringify(updatedErrors) !== JSON.stringify(errors)) {
          this.setState({
            errors: updatedErrors,
          });
        }
      },
    );
  };

  handleDateChange = (date: any, index: any) => {
    try {
      const utcDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
      const selectedDate = utcDate.toISOString();
      const updatedFields = [...this.state.fields];
      updatedFields[index] = {
        ...updatedFields[index],
        imageType: updatedFields[index].imageType || undefined,
        imageUrl: updatedFields[index].imageUrl || undefined,
        receivedDate: selectedDate,
        isActive: true,
      };
      this.setState({
        fields: updatedFields,
      });
    } catch (e: any) {
      console.error(e.message);
    }
  };

  handleBlurEvent = (index: any) => {
    const { fields, errors } = this.state;
    const updatedErrors = { ...errors };
    if (!fields[index].itemCode) {
      updatedErrors[`${index}_itemCode`] = 'Please enter item code.';
    } else {
      delete updatedErrors[`${index}_itemCode`];
    }
    // if (this.props.fields.length != 0) {
    if (
      this.state.dropdown == 'Drape Image' &&
      (fields[index].imageUrl == null || fields[index].imageUrl == '')
    ) {
      updatedErrors[`${index}_imageUrl`] = 'Please enter Image URL.';
    } else {
      updatedErrors[`${index}_imageUrl`] = '';
    }
    // }
    if (JSON.stringify(updatedErrors) !== JSON.stringify(errors)) {
      this.setState({
        errors: updatedErrors,
      });
    }
  };
  handleKeyPress = (event: React.KeyboardEvent<HTMLInputElement>, index: any) => {
    const allowedKeys = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'];
    const { fields, errors } = this.state;
    const updatedErrors = { ...errors };

    if (!allowedKeys.includes(event.key)) {
      updatedErrors[`${index}_itemCode`] = 'Please enter number';
      event.preventDefault();
    } else {
      delete updatedErrors[`${index}_itemCode`];
    }

    this.setState({
      errors: updatedErrors,
    });
  };
  hasIncompleteData = () => {
    const { fields, fields1 } = this.state;

    if (this.props.fields.length !== 0) {
      return fields.every((row: any, index: number) => {
        const formattedDateFields = {
          ...fields[index],
          receivedDate: new Date(fields[index].receivedDate).toLocaleDateString(),
        };

        const formattedDateFields1 = {
          ...fields1[index],
          receivedDate: new Date(fields1[index].receivedDate).toLocaleDateString(),
        };
        if (this.props.fields.length != 0) {
          if (
            this.state.fields[0].imageType == 'Drape Image' &&
            (fields[index].imageUrl == null || fields[index].imageUrl.trim() == '')
          ) {
            return true;
          } else {
            const itemcode = parseInt(formattedDateFields.itemCode, 10);
            const code = parseInt(formattedDateFields1.itemCode, 10);
            return (
              itemcode === code &&
              formattedDateFields.imageUrl === formattedDateFields1.imageUrl &&
              formattedDateFields.imageType === formattedDateFields1.imageType &&
              formattedDateFields.receivedDate === formattedDateFields1.receivedDate
            );
          }
        }
      });
    } else if (this.props.fields.length >= 1 || this.props.fields.length >= 0) {
      return fields.some((row: any) => {
        if (!row.itemCode) {
          return true;
        }
        if (row.imageType === 'Drape Image' && !row.imageUrl) {
          return true;
        }
        return false;
      });
    } else {
      return true;
    }
  };

  render() {
    const defaultSelectedKey =
      this.state.dropdown && this.state.dropdown.length > 0
        ? this.state.dropdown[0].key
        : undefined;
    return (
      <>
        <Grid lg={12} item container spacing={2} direction={'row'}>
          {/* {this.state.itemcodeRemove && (
            <Text className="Itemcodeerror">{this.props.t(`At least one row must remain.`)}</Text>
          )} */}
          {this.state.itemcodeSting && (
            <Text className="itemcodeSting">{this.props.t(`Item code should be in number.`)}</Text>
          )}
          {this.state.errorMessage && (
            <Text className="itemcodeSting">{this.state.errorMessage}</Text>
          )}
          <Grid item lg={8.6} xs={12}></Grid>
          {this.props.fields.length == 0 && (
            <Grid item lg={0.5} xs={12}>
              <Icon
                iconName="SkypeCircleMinus"
                className="iconStyleRemove"
                onClick={() => {
                  if (this.state.selectedItems.length == 0) {
                    return;
                  }
                  if (this.state.selectedItems.length != 0 && this.state.fields.length == 1) {
                    this.setState({ itemcodeRemove: true });
                    setTimeout(() => {
                      this.setState({ itemcodeRemove: false });
                    }, 3000);
                    return;
                  }
                  if (this.state.selectedItems.length != 0) {
                    this.handleRemoveButtonClick();
                  } else {
                    this.setState({ itemcodeRemove: true });
                    setTimeout(() => {
                      this.setState({ itemcodeRemove: false });
                    }, 3000);
                  }
                }}
                title={this.props.t('BTN_REMOVE')}
              />
            </Grid>
          )}
        </Grid>
        <br />
        <form className="myForm" autoComplete="off">
          {Array.isArray(this.state.fields) &&
            this.state.fields?.map((_field: any, index: any) => (
              <div key={index} className={PIMcontentStyles.attributeBody}>
                <Grid lg={12} item container spacing={2} direction={'row'}>
                  {this.props.fields.length == 0 && <Grid item lg={0.5} xs={12} />}
                  {this.props.fields.length == 0 && (
                    <Grid item lg={0.7} xs={12}>
                      <Checkbox
                        className="AddItem"
                        checked={this.state.selectedItems.includes(index)}
                        onChange={() => this.handleCheckboxChange(index)}
                      />
                    </Grid>
                  )}
                  {this.props.fields.length != 0 && <Grid item lg={0.5} xs={12}></Grid>}
                  <Grid item lg={2.8} xs={12}>
                    <Text>
                      {`${t('ITEMCODE')}`}
                      <span style={{ color: 'red' }}>*</span>
                    </Text>
                    <TextField
                      name="itemCode"
                      className="textField"
                      value={this.state.fields[index]?.itemCode || this.state.itemCode || ''}
                      placeholder={`${t('ITEMCODE')}`}
                      onBlur={() => this.handleBlurEvent(index)}
                      onChange={(event: any) => this.handleChangeEvent(event, index)}
                      onKeyPress={(event: React.KeyboardEvent<HTMLInputElement>) =>
                        this.handleKeyPress(event, index)
                      }
                      errorMessage={this.state.errors[`${index}_itemCode`] || ''}
                      disabled={
                        this.props.fields?.length === 1
                          ? this.props.fields[0]?.rejectedreason == null
                            ? false
                            : true
                          : false
                      }
                      // disabled={this.state.SubmitedStatus === 'true' ? true : false}
                    />
                  </Grid>
                  <Grid item lg={2.3} xs={12}>
                    <Text>{`${t('TYPE')}`}</Text>
                    <Dropdown
                      className="imageType"
                      placeholder={`${t('SELECT_TYPE_PALCEHOLDER')}`}
                      options={this.state.dropdown1}
                      defaultSelectedKey={
                        defaultSelectedKey || this.state.fields[index]?.ImagetypeId
                      }
                      onChange={(event, item) => {
                        if (item) {
                          this.setState({ dropdown: item.text }, () => {
                            this.handleChangeDropdown(item, index);
                          });
                        }
                      }}
                      // onBlur={() => this.handleBlurEvent(index)}
                      styles={PIMdropdownStyles}
                    />
                  </Grid>
                  <Grid item lg={2.3} xs={12}>
                    <Text>{`${t('IMAGE')}`}</Text>
                    <TextField
                      name="imageUrl"
                      className="textField"
                      placeholder={`${t('IMAGE')}`}
                      value={this.state.fields[index]?.imageUrl || this.state.imageData || ''}
                      errorMessage={this.state.errors[`${index}_imageUrl`] || ''}
                      onChange={(event: any) => this.handleChangeEvent(event, index)}
                      // disabled={this.state.fields[index]?.imageType === "Drape Image" ? false : true}
                      onBlur={() => this.handleBlurEvent(index)}
                    />
                  </Grid>
                  <Grid item lg={2.3} xs={12}>
                    <Text>{t('RECEIVED_DATE')}</Text>
                    <DatePicker
                      className="receivedDate"
                      placeholder={`${t('Date')}`}
                      showMonthPickerAsOverlay={true}
                      formatDate={(e: any) => e.toLocaleDateString('en-GB')}
                      minDate={this.state.minDate}
                      onBlur={this.form.handleBlurEvent}
                      maxDate={new Date()}
                      onSelectDate={(date: any) => {
                        const selectedDate = new Date(
                          date.getFullYear(),
                          date.getMonth(),
                          date.getDate(),
                        );
                        this.handleDateChange(selectedDate, index);
                      }}
                      value={
                        this.state.fields[index].receivedDate
                          ? new Date(this.state.fields[index].receivedDate)
                          : undefined
                      }
                      showGoToToday={false}
                    />
                  </Grid>
                </Grid>
                <br />
              </div>
            ))}
        </form>
        {this.props.fields.length == 0 && (
          <Grid item lg={2.7} xs={12}>
            <Icon
              iconName="CircleAddition"
              title={this.props.t('ROLE_USER_BTN_ADD')}
              className="iconStyleAdd"
              onClick={this.addNewRow}
            />
          </Grid>
        )}
        <div className={PIMcontentStyles.footer}>
          <Grid lg={12} item container spacing={2} direction={'row'}>
            <Grid item lg={this.props.fields.length == 0 ? 3.5 : 2} xs={12} />
            {this.props.fields.length != 0 && <Grid item lg={1.3} xs={12} />}
            <Grid item lg={2.7} xs={12}>
              {this.props.fields.length == 0 ? (
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonConfirm}
                  type="submit"
                  text={`${t('BTN_SAVE')}`}
                  onClick={() => this.Additem()}
                  disabled={this.hasIncompleteData()}
                />
              ) : (
                <DefaultButton
                  className="button"
                  styles={PIMfooterButtonConfirm}
                  type="submit"
                  disabled={this.hasIncompleteData()}
                  text={`${t('EDIT_BTN_SUBMIT')}`}
                  onClick={() => {
                    this.updateitem();
                    // this.props.ClosePopup();
                  }}
                />
              )}
            </Grid>
            <Grid item lg={2.7} xs={12}>
              <DefaultButton
                className="button"
                styles={PIMfooterButtonCancel}
                text={`${t('BTN_CANCEL')}`}
                onClick={() => this.props.ClosePopup()}
              />
            </Grid>
          </Grid>
        </div>
      </>
    );
  }
}
