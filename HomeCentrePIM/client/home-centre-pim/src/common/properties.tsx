import {
  getTheme,
  IButtonStyles,
  IComboBoxStyles,
  IDropdownStyles,
  IIconProps,
  ISearchBoxStyles,
  IStackItemStyles,
  IStackStyles,
  IStackTokens,
  ITextFieldStyles,
} from '@fluentui/react';

export const uR_button: Partial<IButtonStyles> = {
  root: {
    marginTop: 5,
    color: 'blue',
  },
  rootHovered: {
    backgroundColor: 'skyblue',
  },
};

export const cancelIcon: IIconProps = { iconName: 'Cancel' };
export const rejectIcon: IIconProps = { iconName: 'Cancel' };
export const approveIcon: IIconProps = { iconName: 'Accept' };
export const filterIcon: IIconProps = { iconName: 'Filter' };
export const clearFilter: IIconProps = { iconName: 'ClearFilter' };
export const message: IIconProps = { iconName: 'SkypeMessage' };
export const Remove: IIconProps = { iconName: 'Remove' };
export const DeleteIcon: IIconProps = { iconName: 'Delete' };
export const ChevronRight: IIconProps = { iconName: 'ChevronRight' };
export const ChevronLeft: IIconProps = { iconName: 'ChevronLeft' };

//Stack

export const itemAlignmentsStackStylesForm: IStackStyles = { root: {} };
export const itemAlignmentsStackStyles: IStackStyles = { root: { height: 50 } };
export const stackItemStyles: IStackItemStyles = { root: { padding: 0 } };
export const TextFieldStyles: IStackItemStyles = {
  root: {
    width: 280,
  },
};
export const containerStackTokens: IStackTokens = { childrenGap: 5 };
export const itemAlignmentsStackTokens: IStackTokens = { childrenGap: 5, padding: 10 };

//Searchbox

export const searchBoxStyles: Partial<ISearchBoxStyles> = { root: { width: '100%' } };

//Active state dropdown

export const dropdownStyles: Partial<IDropdownStyles> = { dropdown: { width: '100%' } };
export const dropdownStyles2: Partial<IDropdownStyles> = { dropdown: { width: '100%' } };
export const AssigndropdownStyles: Partial<IDropdownStyles> = {
  dropdown: { width: '200px', fontFamily: 'Montserrat, sans-serif' },
};
export const rejectReasonDropdown: Partial<IDropdownStyles> = {
  dropdown: { width: '100%', position: 'relative', flexWrap: 'wrap', overflow: 'auto' },
};

export const submitButton: Partial<IButtonStyles> = {
  root: {
    backgroundColor: '#08ad26',
    borderColor: 'green',
  },
  rootHovered: {
    backgroundColor: 'darkgreen',
  },
};
export const deactivateButton: Partial<IButtonStyles> = {
  root: {
    width: '100%',
  },
};

export const searchButton: Partial<IButtonStyles> = {
  root: {
    padding: 5,
    width: '100%',
    backgroundColor: 'white',
    color: 'blue',
  },
};

export const cancelButton: Partial<IButtonStyles> = {
  root: {
    color: 'white',
    backgroundColor: 'Red',
  },
  rootHovered: {
    backgroundColor: 'darkred',
  },
};

const theme = getTheme();
export const iconButtonStyles: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.black,
    // marginLeft: 'auto',
    // marginRight: '1px',
    // marginTop: '-47px',
    position: 'absolute',
    top: 0,
    right: 0,
  },
  rootHovered: {
    color: theme.palette.black,
  },
};
export const iconButtonStyles1: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.black,
    backgroundColor: '#e0e0e0',
    position: 'absolute',
    top: 0,
    right: 0,
    zIndex: 10,
  },
  rootHovered: {
    color: theme.palette.black,
  },
};
export const iconButtonStyles12: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.black,
    backgroundColor: '#e0e0e0',
    position: 'absolute',
    right: 0,
    top: '40%',
    transform: 'translateY(40%)',
    zIndex: 10,
    bottom: 0,
  },
  rootHovered: {
    color: theme.palette.black,
  },
};
export const iconButtonStyles123: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.black,
    backgroundColor: '#e0e0e0',
    position: 'absolute',
    left: 0,
    top: '50%',
    transform: 'translateY(-50%)',
    zIndex: 10,
  },
  rootHovered: {
    color: theme.palette.black,
  },
};
export const UploadimageStyles: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.neutralPrimary,
    marginLeft: 'auto',
    marginRight: '-11px',
    marginTop: '-36px',
  },
  rootHovered: {
    color: theme.palette.neutralDark,
  },
};
export const UserMarketplaceStyles: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.neutralPrimary,
    marginLeft: 'auto',
    marginRight: '-10px',
    marginTop: '-65px',
  },
  rootHovered: {
    color: theme.palette.neutralDark,
  },
};
export const PlanoCancelStyles: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.neutralPrimary,
    marginLeft: 'auto',
    width: '39px',
    marginRight: '2px',
    marginTop: '-43px',
  },
  rootHovered: {
    color: theme.palette.neutralDark,
  },
};
export const PlanoDisStyles: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.neutralPrimary,
    marginLeft: '190px',
    marginRight: '-36px',
    marginTop: '-37px',
  },
  rootHovered: {
    color: theme.palette.neutralDark,
  },
};
export const UploadStyles: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.neutralPrimary,
    marginLeft: 'auto',
    marginRight: '-10px',
    marginTop: '-43px',
  },
  rootHovered: {
    color: theme.palette.neutralDark,
  },
};
export const UploadStyles1: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.neutralPrimary,
    marginLeft: '50px',
    marginRight: '-1px',
    marginTop: '-43px',
  },
  rootHovered: {
    color: theme.palette.neutralDark,
  },
};
export const iconButtonStyless: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.neutralPrimary,
    marginLeft: 'auto',
    marginRight: '2px',
    border: '2px solid black',
    margin: '2px',
    borderRadius: '60px',
  },
  rootHovered: {
    color: theme.palette.white,
    background: 'red',
  },
};
export const iconButtonStylesssClicked1: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.neutralPrimary,
    // background: '#03b30f',
    marginLeft: 'auto',
    marginRight: '2px',
    margin: '2px',
    border: '2px solid black',
    borderRadius: '60px',
  },
  rootHovered: {
    color: theme.palette.white,
    background: '#03b30f',
  },
};

export const iconButtonStyless1: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.neutralPrimary,
    marginLeft: 'auto',
    marginRight: '2px',
    border: '2px solid black',
    margin: '2px',
    borderRadius: '60px',
    background: 'red',
  },
  // rootHovered: {
  //   color: theme.palette.white,
  //   background: 'white',
  // },
};
export const MarketplaceStyles: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.neutralPrimary,
    marginLeft: 'auto',
    marginRight: '-11px',
  },
  rootHovered: {
    color: theme.palette.neutralDark,
  },
};

export const AttributeStyles: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.neutralPrimary,
    marginLeft: 'auto',
    marginRight: '-12px',
    marginTop: '-43px',
  },
  rootHovered: {
    color: theme.palette.neutralDark,
  },
};

export const iconButtonStylessClicked: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.white,
    background: 'red',
    marginLeft: 'auto',
    marginRight: '2px',
    border: '2px solid black',
    margin: '2px',
    borderRadius: '60px',
  },
};
export const iconButtonStylesss: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.neutralPrimary,
    marginLeft: 'auto',
    marginRight: '2px',
    margin: '2px',
    border: '2px solid black',
    borderRadius: '60px',
  },
  rootHovered: {
    color: theme.palette.white,
    background: '#03b30f',
  },
};
export const iconButtonStylesssClicked: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.white,
    background: '#03b30f',
    marginLeft: 'auto',
    marginRight: '2px',
    margin: '2px',
    border: '2px solid black',
    borderRadius: '60px',
  },
  rootHovered: {
    color: theme.palette.white,
    background: '#03b30f',
  },
  rootPressed: {
    color: theme.palette.white,
    background: '#03b30f',
  },
};

export const rejectComment: Partial<ITextFieldStyles> = {
  fieldGroup: {
    height: '100%',
  },
};

//UID STYLES

export const createPlanogram: IStackStyles = {
  root: {
    padding: 10,
  },
};

export const uidField: IStackStyles = {
  root: {
    padding: 0,
    marginTop: '10%',
  },
};
export const departmentRead: Partial<ITextFieldStyles> = {
  fieldGroup: { boxShadow: '0 5px 10px rgb(0 0 0 / 0.4)' },
};
export const departmentSelect: Partial<IDropdownStyles> = {
  dropdown: {
    boxShadow: '0 5px 10px rgb(0 0 0 / 0.4)',
    borderColor: 'white',
    width: '100%',
  },
  caretDown: { display: 'none' },
  dropdownOptionText: { padding: '8px' },
};
export const textField: Partial<ITextFieldStyles> = {
  fieldGroup: {
    width: '100%',
  },
};

export const columnFilter: Partial<IComboBoxStyles> = {
  root: {
    maxWidth: 10,
    backgroundColor: 'white',
    borderColor: 'white',
    zIndex: 2,
    cursor: 'pointer',
    opacity: 0,
    position: 'relative',
    top: '-10px',
  },
  input: {
    backgroundColor: 'white',
    borderColor: 'white',
  },
  rootHovered: {
    backgroundColor: 'lightgrey',
    borderColor: 'white',
  },
  optionsContainerWrapper: {
    width: '250px',
  },
};
export const filterButton: Partial<IComboBoxStyles> = {
  root: {
    position: 'absolute',
    top: '0px',
    left: '100px',
    height: '90%',
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
    background: 'none',
  },
  rootHovered: {
    background: 'none',
  },
};
export const CancelButtonStyles: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.neutralPrimary,
    marginLeft: 'auto',
    marginRight: '2px',
    marginTop: '10px',
  },
  rootHovered: {
    color: theme.palette.neutralDark,
  },
};
export const CancelStyles: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.neutralPrimary,
    marginLeft: 'auto',
    marginRight: '-8px',
    marginTop: '10px',
  },
  rootHovered: {
    color: theme.palette.neutralDark,
  },
};
export const CancelUserStyles: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.neutralPrimary,
    margin: '-4px -31px 3px 0px',
  },
  rootHovered: {
    color: theme.palette.neutralDark,
  },
};
export const CancelroleStyles: Partial<IButtonStyles> = {
  root: {
    color: theme.palette.neutralPrimary,
    margin: '-9px -31px 3px 0px',
  },
  rootHovered: {
    color: theme.palette.neutralDark,
  },
};
