import {
  FontWeights,
  getTheme,
  IButtonStyles,
  ICheckboxStyles,
  IComboBoxStyles,
  IDropdownStyles,
  IIconProps,
  IModalStyles,
  ISearchBoxStyles,
  mergeStyles,
  mergeStyleSets,
} from '@fluentui/react';
import { colors } from '@material-ui/core';

const fontFamily = 'Montserrat, sans-serif';
const theme = getTheme();
export const PIMButtons: IButtonStyles = {
  root: {
    padding: 5,
    width: '100%',
    backgroundColor: '#008B8B',
    color: 'White',
    marginBottom: '6px',
  },
  rootHovered: {
    backgroundColor: 'white',
    color: 'black',
  },
};
export const PIMHearderText = {
  root: {
    fontSize: '23px',
  },
};
export const PIMButtons1: IButtonStyles = {
  root: {
    width: '30%',
    backgroundColor: '#008B8B',
    color: 'White',
  },
  rootHovered: {
    backgroundColor: 'white',
    color: 'black',
  },
};
export const DeactivateButton: IButtonStyles = {
  root: {
    padding: 5,
    width: '100%',
  },
};
export const PIMHeaderButtonsQc: IButtonStyles = {
  root: {
    padding: 5,
    width: '85%',
  },
};

export const PIMButtonsQc: IButtonStyles = {
  root: {
    padding: 5,
    // marginLeft: '-12%',
    width: '85%',
    backgroundColor: '#008B8B',
    color: 'White',
    marginBottom: '6px',
  },
  rootHovered: {
    backgroundColor: 'white',
    color: 'black',
  },
};

export const PIMcontentStylesQC = mergeStyleSets({
  container: {
    display: 'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    width: '70%',
    minHeight: '40%',
    height: 'auto',
  },
  QcconfirmContainer: {
    display: 'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    width: '821px',
    minHeight: '100px',
    height: 'auto',
    borderColor: 'black',
  },
});

export const PIMdropdownStylesDd: Partial<IDropdownStyles> = {
  dropdown: { width: '100%', marginTop: '-2.5%', fontFamily: `${fontFamily}` },
  callout: { minWidth: '150px' },
  dropdownItem: { fontFamily: `${fontFamily}` },
};

export const PIMdropdownStylesQCTab: Partial<IDropdownStyles> = {
  dropdown: {
    width: '89%',
    marginTop: '-39.5%',
    fontFamily: `${fontFamily}`,
    top: '4px',
    marginLeft: '5px',
  },
  callout: {
    minWidth: '150px',
  },
  dropdownItem: { fontFamily: `${fontFamily}` },
};

export const PIMHeaderButtons: IButtonStyles = {
  root: {
    padding: 5,
    width: '100%',
  },
};
export const DeleteButton: IButtonStyles = {
  root: {
    padding: 5,
    width: '100%',
    backgroundColor: 'white',
    color: 'red',
    borderColor: 'red',
  },
  rootHovered: {
    backgroundColor: 'red',
    color: 'white',
    borderColor: 'red',
  },
};

export const BorderShadowButton: Partial<IButtonStyles> = {
  root: {
    color: '#008B8B',
    boxShadow: '0 5px 10px rgb(0 0 0 / 0.4)',
    borderColor: 'white',
    width: '100%',
  },
};

export const searchIcon: IIconProps = { iconName: 'Search' };
export const removeIcon: IIconProps = { iconName: 'Delete' };

export const PIMattributeSelect: IButtonStyles = {
  root: {
    padding: 5,
    width: '100%',
    backgroundColor: 'white',
    borderColor: 'white',
  },
};

export const PIMsearchBoxStyles: Partial<ISearchBoxStyles> = {
  root: {
    width: '100%',
    boxShadow: '0 5px 10px rgb(0 0 0 / 0.4)',
    // borderColor: 'white',
    marginBottom: 10,
  },
};
export const PIMDropdownStyles: Partial<IDropdownStyles> = {
  root: {
    whiteSpace: 'nowrap',
  },
};

export const PIMRemoveButton: Partial<IButtonStyles> = {
  root: {
    color: 'red',
  },
  rootHovered: {
    backgroundColor: 'darkred',
    color: 'white',
  },
};
export const PIMfooterButtonCancel: Partial<IButtonStyles> = {
  root: {
    width: '100%',
    boxShadow: '0 5px 10px rgb(0 0 0 / 0.4)',
    // borderColor: 'red',
    // backgroundColor: 'red',
    // borderColor: 'white',
    backgroundColor: '#008B8B',
    color: 'white',
  },
  rootHovered: {
    // backgroundColor: 'darkred',
    // color: 'white',
    backgroundColor: 'white',
  },
};
export const customModalStyles: Partial<IModalStyles> = {
  main: {
    width: '32%',
    maxWidth: 'none',
  },
};
export const PIMfooterButtonCancel1: Partial<IButtonStyles> = {
  root: {
    width: '100%',
    boxShadow: '0 5px 10px rgb(0 0 0 / 0.4)',
    // borderColor: 'red',
    // backgroundColor: 'red',
    // borderColor: 'white',
    backgroundColor: '#008B8B',
    color: 'white',
    marginTop: '13px',
  },
  rootHovered: {
    // backgroundColor: 'darkred',
    // color: 'white',
    backgroundColor: 'white',
  },
};
export const iconButtonStylesImg: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.neutralPrimary,
    marginLeft: 'auto',
    backgroundColor: 'lighblue',
    fontWeight: '999',
    marginRight: '2.5px',
    // marginTop: '-1px',
  },
  rootHovered: {
    color: theme.palette.neutralDark,
  },
};
export const columndata = mergeStyles({
  fontSize: '15px',
  fontFamily: `${fontFamily}`,
});
export const PIMfooterButtonConfirm: Partial<IButtonStyles> = {
  root: {
    width: '100%',
    boxShadow: '0 5px 10px rgb(0 0 0 / 0.4)',
    // borderColor: 'white',
    backgroundColor: '#008B8B',
    // backgroundColor: '#08ad26',
    color: 'white',
  },
  rootHovered: {
    backgroundColor: 'white',
  },
};
export const PIMfooterButtonConfirm1: Partial<IButtonStyles> = {
  root: {
    width: '100%',
    boxShadow: '0 5px 10px rgb(0 0 0 / 0.4)',
    marginTop: '10%',
    // borderColor: 'white',
    backgroundColor: '#008B8B',
    // backgroundColor: '#08ad26',
    color: 'white',
  },
  rootHovered: {
    backgroundColor: 'white',
  },
};
export const DataButton: Partial<IButtonStyles> = {
  root: {
    marginLeft: '-20%',
    marginRight: '30%',
  },
};
export const CheckboxStyles: Partial<ICheckboxStyles> = {
  root: {
    marginLeft: '20px',
  },
};

export const activeText = mergeStyles({
  alignContent: 'center',
  fontWeight: 'bold',
  fontSize: '18px',
  width: '100%',
  fontFamily: `${fontFamily}`,
  color: 'white',
  display: 'flex',
  justifyContent: 'center',
});
export const activeText1 = mergeStyles({
  alignContent: 'center',
  fontWeight: 'bold',
  fontSize: '18px',
  width: '100%',
  marginTop: '10px',
  fontFamily: `${fontFamily}`,
  color: 'black',
  display: 'flex',
  justifyContent: 'center',
});
export const PIMDropdownStyles1: Partial<IDropdownStyles> = {
  root: {
    whiteSpace: 'nowrap',
  },
  callout: {
    minWidth: '310px',
  },
};

export const PIMDropdownStyles3: Partial<IDropdownStyles> = {
  root: {
    whiteSpace: 'nowrap',
  },
  callout: {
    minWidth: '99px',
  },
};
export const PIMDropdownStyles4: Partial<IDropdownStyles> = {
  root: {
    whiteSpace: 'nowrap',
  },
  callout: {
    minWidth: '138px',
  },
};
export const PIMDropdownStyles11: Partial<IDropdownStyles> = {
  root: {
    whiteSpace: 'nowrap',
  },
  callout: {
    minWidth: '310px',
  },
};
export const PIMdropdownStyles: Partial<IDropdownStyles> = {
  dropdown: { width: '100%', fontFamily: `${fontFamily}` },
  callout: {
    minWidth: '230px',
  },
  dropdownItem: { fontFamily: `${fontFamily}` },
};
export const PIMdropdownStyles2: Partial<IDropdownStyles> = {
  dropdown: { width: '100%', marginTop: '-1.5%', fontFamily: `${fontFamily}`, top: '4px' },
  callout: {
    minWidth: '150px',
  },
  dropdownItem: { fontFamily: `${fontFamily}` },
};
export const PIMdropdownStyles1: Partial<IDropdownStyles> = {
  dropdown: { width: '100%', fontFamily: `${fontFamily}` },
  callout: {
    minWidth: '150px',
  },
  dropdownItem: { fontFamily: `${fontFamily}` },
};
export const PIMdropdownStyles21: Partial<IDropdownStyles> = {
  dropdown: { width: '100%', marginTop: '-2.2%', fontFamily: `${fontFamily}`, top: '3.1px' },
  callout: {
    minWidth: '150px',
  },
  dropdownItem: { fontFamily: `${fontFamily}` },
};

export const PIMdropdownStyles22: Partial<IDropdownStyles> = {
  dropdown: { width: '100%', marginTop: '-2.2%', fontFamily: `${fontFamily}`, top: '-0.8px' },
  callout: {
    minWidth: '150px',
  },
  dropdownItem: { fontFamily: `${fontFamily}` },
};

export const productDropdown: Partial<IDropdownStyles> = {
  dropdown: { width: '100%', fontFamily: `${fontFamily}` },
  callout: {
    minWidth: '300px',
  },
  dropdownItem: { width: '200%', fontFamily: `${fontFamily}` },
};
export const AttributeheadDropdown: Partial<IDropdownStyles> = {
  dropdown: {
    width: '100%',
    backgroundColor: 'blue',
    color: 'blue',
    fontWeight: 'bold',
    fontFamily: `${fontFamily}`,
  },
  dropdownItem: { fontFamily: `${fontFamily}` },
};
export const clearBtnStyle: Partial<IButtonStyles> = { root: { width: '425%' } };
export const PIMComboBoxStyles: Partial<IComboBoxStyles> = {
  root: {
    width: '100%',
    fontFamily: `${fontFamily}`,
  },
  optionsContainerWrapper: {
    width: '200px',
    fontFamily: `${fontFamily}`,
  },
};
export const PIMMessageBarStyle: Partial<any> = {
  root: {
    position: 'absolute',
    zIndex: 9999999,
    boxShadow: '0 0 3px 1px #afafaf',
    width: 'auto',
    alignContent: 'center',
    left: '40%',
    float: 'center',
    height: '20px',
    minWidth: '40px',
  },
};
export const EditButtons: Partial<any> = {
  root: {
    fontWeight: 'bold',
  },
};
export const PIMrootClass = mergeStyles({ width: '100%' });
export const Column = mergeStyles({
  backgroundColor: 'grey',
  color: 'grey',
});
export const HeadText = mergeStyles({ color: 'blue', fontFamily: `${fontFamily}` });
export const columnHeader = mergeStyles({
  fontWeight: 'bold',
  fontSize: '15px',
  fontFamily: `${fontFamily}`,
});
export const columnHeaderPlano = mergeStyles({
  fontWeight: 'bold',
  fontSize: '15px',
  fontFamily: `${fontFamily}`,
  paddingTop: '4px',
  paddingBottom: '4px',
});
export const hyperlink = mergeStyles({
  color: 'blue',
  cursor: 'pointer',
  fontWeight: 'bold',
  transition: 'transform 0.09s ease-in-out',
  selectors: {
    '&:hover': {
      textDecoration: 'underline',
      color: 'blue',
    },
  },
});

export const HeadInfo = mergeStyles({
  color: 'black',
  fontFamily: `${fontFamily}`,
  fontWeight: 'bold',
  marginTop: '18px',
});

export const PIMcontentStyles = mergeStyleSets({
  Approver: {
    display: 'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    minWidth: '30%',
    width: 'auto',
    minHeight: '10%',
    height: 'auto',
    borderColor: 'black',
  },
  container: {
    display: 'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    width: '30%',
    minHeight: '40%',
    height: 'auto',
  },
  container4: {
    display: 'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    borderRadius: '4px',
    width: '382px',
    minHeight: '200px',
    height: 'auto',
  },
  container5: {
    display: 'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    width: '410px',
    minHeight: '255px',
    height: 'auto',
  },
  confirmContaineruser: {
    display: 'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    minWidth: '26%',
    width: 'auto',
    minHeight: '19%',
    height: 'auto',
    borderColor: 'black',
  },
  confirmContaineruser1: {
    // display: 'flex',
    // flexFlow: 'column nowrap',
    // alignItems: 'stretch',
    // minWidth: '26%',
    // width: 'auto',
    // minHeight: '19%',
    // height: 'auto',
    // borderColor: 'black',
    minHeight: '132px',
    minWidth: '355px',
  },
  confirmContaineruser2: {
    minHeight: '132px',
    minWidth: '360px',
  },
  container1: {
    display: 'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    width: '30%',
    minHeight: '30%',
    height: 'auto',
  },
  attributeContainer: {
    display: 'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    width: '478px',
    minHeight: '408px',
    height: 'auto',
  },
  attributeContainer1: {
    display: 'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    width: '478px',
    minHeight: '303px',
    height: 'auto',
  },
  tableContainer: {
    display: 'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    minWidth: '680px' /* 50% of 1920px */,
    minHeight: '315px' /* 50% of 1080px */,
    /* Keep percentage for max height */
    height: 'auto',
    marginTop: '4%',
  },
  tableContainer1: {
    display: 'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    minWidth: '680px',
    height:'600px',
    marginTop: '4%',
  },
  tableContainerReject: {
    display: 'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    minWidth: '680px',
    width: 'auto',
    borderRadius: '4px',
    minHeight: '317px',
    height: 'auto',
    marginTop: '4%',
  },
  confirmContainer: {
    display: 'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    borderRadius: '4px',
    minWidth: '383px',
    width: 'auto',
    minHeight: '100px',
    height: 'auto',
    borderColor: 'black',
  },
  confirmContainerStudio: {
    display: 'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    borderRadius: '4px',
    width: '478px',
    minHeight: '100px',
    height: 'auto',
    borderColor: 'black',
    position: 'fixed',
  },
  // confirmContainer1: {
  //   display: 'flex',
  //   flexFlow: 'column nowrap',
  //   alignItems: 'stretch',
  //   minWidth: '28%',
  //   width: 'auto',
  //   minHeight: '10%',
  //   height: 'auto',
  //   borderColor: 'black',
  // },
  confirmContainer1: {
    display: 'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    minWidth: '35%',
    width: 'auto',
    minHeight: '25%',
    height: 'auto',
    borderColor: 'black',
    borderRadius: '2px',
  },
  ExcelContainer: {
    display :'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    overflowY: 'hidden',
    minWidth: '546px',
    height: '425px',
  },
  ExcelContainer1: {
    display: 'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    minWidth: '40%',
    minheight: '-30%',
  },
  ExcelContainer2: {
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    overflowY: 'hidden',
    minWidth: '546px',
    height: '411px',
  },
  ExcelContainerPS: {
    display: 'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    minWidth: '35%',
    minheight: '-100%',
  },
  confirmbodyplano: {
    flex: '4 4 auto',
    overflowY: 'hidden',
    display: 'block',
    alignItems: 'center',
    width: 'auto',
    marginTop: '5%',
    marginLeft: '-52%',
    marginRight: '2%',
  },
  header: [
    theme.fonts.xLargePlus,
    {
      flex: '1 1 auto',
      color: theme.palette.neutralPrimary,
      display: 'flex',
      alignItems: 'center',
      fontWeight: FontWeights.semibold,
      padding: '9px 14px 2px 14px',
      height: '60px',
    },
  ],
  header2: [
    theme.fonts.xLargePlus,
    {
      flex: '1 1 auto',
      color: theme.palette.neutralPrimary,
      display: 'flex',
      alignItems: 'center',
      fontWeight: FontWeights.semibold,
      padding: '9px 7px px 5px',
      height: '30px',
    },
  ],
  header1: [
    theme.fonts.xLargePlus,
    {
      flex: '1 1 auto',
      color: theme.palette.neutralPrimary,
      display: 'flex',
      alignItems: 'center',
      fontWeight: FontWeights.semibold,
      padding: '17px 0px 9px 24px',
      height: '60px',
    },
  ],
  popupHeader: [
    theme.fonts.xLargePlus,
    {
      flex: '1 1 auto',
      color: theme.palette.neutralPrimary,
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      fontWeight: FontWeights.semibold,
      padding: '9px 14px 2px 14px',
      height: '60px',
    },
  ],
  confirmHeaderUser: [
    {
      flex: '1 1 auto',
      alignItems: 'center',
      height: '21%',
      padding: '10px 0px 0px 322px',
      marginBottom: '-10%',
    },
  ],
  confirmHeaderUser1: [
    {
      // flex: '1 1 auto',
      // alignItems: 'center',
      // height: '21%',
      // padding: '10px 0px 0px 322px',
      // marginBottom: '-10%',
      textAlign: 'end',
      padding: '10px',
    },
  ],
  confirmHeaderUser2: [
    {
      // flex: '1 1 auto',
      // alignItems: 'center',
      // height: '21%',
      // padding: '10px 0px 0px 322px',
      // marginBottom: '-10%',
      textAlign: 'end',
      padding: '10px',
    },
  ],
  confirmHeader: [
    {
      flex: '1 1 auto',
      alignItems: 'center',
      height: '10px',
      padding: '12px 12px 14px 24px',
      display: 'flex',
      marginBottom: '-10%',
    },
  ],
  body: {
    flex: '4 4 auto',
    overflowY: 'hidden',
    display: 'block',
    alignItems: 'center',
    width: 'auto',
    height: 'auto',
    minHeight: '35%',
    marginLeft: '5%',
    marginRight: '5%',
    marginBottom: '5%',
    fontFamily: 'Montserrat, sans-serif',
  },
  attributeBody: {
    flex: '4 4 auto',
    display: 'block',
    alignItems: 'center',
    width: '100%',
    overflowY: 'hidden',
  },
  attributeBody1: {
    marginLeft: '43px',
    fontWeight: 'bold',
    marginBottom: '10px',
  },
  attributeBody12: {
    marginLeft: '35px',
    fontWeight: 'bold',
    marginBottom: '10px',
  },
  attributeBodyStudio: {
    marginTop: '2%',
    marginLeft: '38px',
    fontWeight: 'bold',
    marginBottom: '-8px',
  },
  attributeBodyStudioActive: {
    // marginTop: '-1%',
    marginLeft: '38px',
    fontWeight: 'bold',
    // marginBottom: '-5px',
  },
  attributeBody2: {
    marginLeft: '32px',
    fontWeight: 'bold',
    marginBottom: '10px',
  },
  confirmbody: {
    flex: '4 4 auto',
    overflowY: 'hidden',
    display: 'block',
    alignItems: 'center',
    width: 'auto',
    marginTop: '10%',
    marginLeft: '5%',
    marginRight: '2%',
  },
  confirmbody4: {
    textAlign: 'center',
    marginTop: '-10px',
  },
  confirmbody5: {
    textAlign: 'center',
    marginTop: '5px',
  },
  confirmbody6: {
    textAlign: 'center',
    marginTop: '5px',
    width: 'auto',
    marginLeft: '5px',
    marginRight: '5px',
  },
  confirmbody7: {
    textAlign: 'center',
    marginTop: '0px',
    paddingLeft: '37px',
    paddingRight: '28px'
  },
  confirmbody8: {
    textAlign: 'center',
    marginTop: '-5px',
    paddingLeft: '25px',
    paddingRight: '25px'
  },
  confirmbody1: {
    flex: '4 4 auto',
    overflowY: 'hidden',
    display: 'block',
    alignItems: 'center',
    width: 'auto',
    marginTop: '5%',
    marginBottom: '2%',
    marginRight: '2%',
  },
  confirmbody2: {
    flex: '4 4 auto',
    overflowY: 'hidden',
    display: 'block',
    alignItems: 'center',
    width: 'auto',
    marginTop: '10%',
    marginLeft: '8%',
    marginRight: '2%',
    minWidth: '38%',
    minHeight: '20%',
  },
  confirmbody3: {
    flex: '4 4 auto',
    overflowY: 'hidden',
    display: 'block',
    alignItems: 'center',
    width: 'auto',
    marginTop: '10%',
    marginLeft: '15%',
    marginRight: '2%',
    minWidth: '38%',
    minHeight: '20%',
  },
  rejectPopup: {
    flex: '4 4 auto',
    overflowY: 'hidden',
    display: 'block',
    alignItems: 'center',
    width: '85%',
    height: 'auto',
    minHeight: '35%',
    marginLeft: '8%',
    marginRight: '5%',
    marginBottom: '5%',
    fontFamily: 'Montserrat, sans-serif',
  },
  progressBarModal: {
    width: '30vw !important',
    height: '10vh !important',
    minHeight: '0px !important',
  },
  footer: [
    theme.fonts.xLargePlus,
    {
      flex: '1 1 auto',
      color: theme.palette.neutralPrimary,
      display: 'flex',
      alignItems: 'center',
      fontWeight: FontWeights.semibold,
      padding: '12px 12px 14px 24px',
      width: 'auto',
      // minWidth: '50%',
      // marginLeft: '20%',
      // marginRight: '20%',
    },
  ],
  footers: [
    theme.fonts.xLargePlus,
    {
      flex: '1 1 auto',
      color: theme.palette.neutralPrimary,
      display: 'flex',
      alignItems: 'center',
      fontWeight: FontWeights.semibold,
      padding: '0px 0px 20px 0px',
      width: 'auto',
    },
  ],
  editFooter: [
    theme.fonts.xLargePlus,
    {
      flex: '1 1 auto',
      color: theme.palette.neutralPrimary,
      display: 'flex',
      alignItems: 'center',
      fontWeight: FontWeights.semibold,
      padding: '12px 12px 14px 24px',
      width: 'auto',
      minWidth: '50%',
    },
  ],
  Excelfooter: {
    flex: '1 1 auto',
    color: theme.palette.neutralPrimary,
    display: 'flex',
    alignItems: 'center',
    fontWeight: FontWeights.semibold,
    padding: '12px 12px 14px 24px',
    width: 'auto',
    marginLeft: '20%',
    marginRight: '20%',
  },
  imageContainer: {
    display: 'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    minWidth: '500px',
    width: 'auto',
    minHeight: '500px',
    height: 'auto',
  },
  fullImageContainer: {
    display: 'flex',
    flexFlow: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    width: 'auto',
    height: 'auto',
    maxWidth: '100%',
    maxHeight: '100%',
  },
  imageContainerPhotoshote: {
    display: 'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    minWidth: '400px',
    width: '820px',
    minHeight: '500px',
    height: 'auto',
  },
  SaveContainer: {
    display: 'flex',
    flexFlow: 'column nowrap',
    alignItems: 'stretch',
    minWidth: '500px',
    width: 'auto',
    minHeight: '150px',
    height: 'auto',
    borderRadius: '4px',
  },
  imagebody: {
    flex: '4 4 auto',
    // overflowY: 'auto',
    display: 'block',
    alignItems: 'center',
    width: 'auto',
    height: 'auto',
    minHeight: '300px',
    minWidth: '300px',
    marginLeft: '5%',
    marginRight: '5%',
    marginBottom: '5%',
  },
});

export const PIMsearchBoxStyles123: Partial<ISearchBoxStyles> = {
  root: {
    boxShadow: '0 5px 10px rgb(0 0 0 / 0.4)',
    borderColor: 'white',
    marginLeft: '6.6%',
    maxWidth: '85%',
    width: '100%',
  },
};
export const PIMButtons123: IButtonStyles = {
  root: {
    // marginTop: '100%',
    width: '100%',
    backgroundColor: '#008B8B',
    color: 'White',
  },
  rootHovered: {
    backgroundColor: 'white',
    color: 'black',
  },
};

export const PIMuploadInput = mergeStyles({
  position: 'relative',
  maxWidth: '400px',
  height: '46px',
  zIndex: 2,
  cursor: 'pointer',
  opacity: 0,
  width: '100%',
});

export const PIMuploadButton: Partial<IButtonStyles> = {
  root: {
    position: 'absolute',
    top: '0px',
    left: '0px',
    width: '100%',
    height: '100%',
    zIndex: 1,
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    color: 'blue',
    backgroundColor: 'white',
    fontSize: '1.1rem',
    cursor: 'pointer',
    borderRadius: '4px',
    outline: 'none',
    transitionTimingFunction: 'backgroundColor 0.4s',
    boxShadow: '0px 8px 24px rbga(149, 157, 165, 0.4)',
  },
};
export const PIMfileUpload = mergeStyleSets({
  fileCard: {
    border: '1px grey',
    minHeight: '200px',
    width: '100%',
    height: '50%',
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    flexDirection: 'column',
  },
  fileInput: {
    position: 'relative',
    marginBottom: '1.5em',
    marginLeft: '15%',
    marginRight: '15%',
  },
  listItem: {
    backgroundColor: 'lightgrey',
    borderRadius: '5px',
    display: 'flex',
    alignItems: 'center',
    width: '100%',
    height: '50px',
    marginBottom: 5,
  },
  img: {
    width: 'auto',
    height: '60%',
    objectFit: 'contain',
    margin: 0,
    padding: 0,
    marginLeft: 5,
    flex: 2,
  },
  p: {
    flex: 2,
    fontSize: '0.9em',
    marginLeft: 5,
    marginTop: 10,
    width: '90%',
  },
  actions: {
    justifySelf: 'flexEnd',
    cursor: 'pointer',
  },
  fileList: {
    width: '100%',
    height: '2%',
    marginRight: '',
    marginLeft: '',
  },
});

export const chatBoxStyles = mergeStyleSets({
  border: '2px solid rgba(0, 0, 0, 0.05)',
  height: '40%',
  padding: '10px',
  backgroundColor: '#bbdaf8',
});
export const boxStyles = mergeStyleSets({
  border: '2px solid #797775',
  height: 'auto',
  padding: '10px',
  marginTop: '10px',
});
export const ImgIcons = mergeStyleSets({
  width: '85%',
  marginTop: '6px',
});
export const ImgIcons1 = mergeStyleSets({
  width: '50%',
});
export const customTableStyles1 = {
  rows: {
    style: {
      backgroundColor: 'rgb(237, 243, 241)',
    },
  },
};
export const Alartmessage = {
  color: 'red',
  fontFamily: 'Montserrat, sans-serif',
  fontSize: '18px',
  borderRadius: '10px',
};

export const uiderrorcomp2: IButtonStyles = {
  root: {
    color: 'green',
    backgroundColor: 'white',
  },
  rootHovered: {
    color: 'green !important',
    backgroundColor: 'white !important',
  },
};

export const uiderrorcomp1: IButtonStyles = {
  root: {
    color: 'red',
    backgroundColor: 'white',

  },
  rootHovered: {
  color: 'red !important',
  backgroundColor: 'white !important',
  },
};
export const defaultStyles: IButtonStyles = {
  root: {
    color: 'black', // Default color
  },
};

export const expandableTable = mergeStyleSets({
  countCols : {
    border: '1px solid #ddd',
    padding: '8px 16px',
    borderLeft: 'none',
    borderRight: 'none',
    minWidth: '100px',
    display: 'flex',
    alignItems: 'center',
    flexGrow: 1,
    WebkitFlexBasis: 0,
  },
  planoDivCol : {
    border: '1px solid #ddd',
    padding: '8px 16px',
    borderLeft: 'none',
    borderRight: 'none',
    minWidth: '105px',
    wordBreak: 'break-all',
    whiteSpace: 'pre-wrap',
    flexGrow: 1,
    WebkitFlexBasis: 0,
  },
  uidDivCol: {
    border: '1px solid #ddd',
    padding: '8px 16px',
    borderLeft: 'none', borderRight: 'none',
    minWidth: '150px',
    wordWrap: 'break-word',
    flexGrow: 1,
    WebkitFlexBasis: 0,
  },
  expandIconCol: {
    border: '1px solid #ddd',
    padding: '8px 16px',
    borderLeft: 'none',
    borderRight: 'none',
    width: '48px',
  },
  table: {
    width: '100%',
    borderCollapse: 'collapse',
    fontSize: '12px',
    maxWidth: '100%'
  },
  row: {
    display: 'flex'
  },
});
