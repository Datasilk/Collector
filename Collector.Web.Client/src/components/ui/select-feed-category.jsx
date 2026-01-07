import React, { useState } from 'react';
import Select from '../forms/select.jsx';
import Input from '../forms/input.jsx';
import Icon from './icon.jsx';

function SelectFeedCategory({ categories, selectedCategoryId, onSelect, onAddCategory }) {
  const [isAddingCategory, setIsAddingCategory] = useState(false);
  const [newCategoryName, setNewCategoryName] = useState('');

  const handleAddCategoryClick = () => {
    setIsAddingCategory(true);
  };

  const handleCreateCategory = () => {
    if (newCategoryName.trim()) {
      onAddCategory(newCategoryName);
      setIsAddingCategory(false);
      setNewCategoryName('');
    }
  };

  const handleCancel = () => {
    setIsAddingCategory(false);
    setNewCategoryName('');
  };

  if (categories.length === 0 || isAddingCategory) {
    return (
      <div className="form-group">
        <Input 
          label="Category Name" 
          value={newCategoryName} 
          onChange={(e) => setNewCategoryName(e.target.value)} 
        />
        {newCategoryName && (
          <div className="buttons">
            <button onClick={handleCreateCategory}>Create Category</button>
            <button className="cancel" onClick={handleCancel}>Cancel</button>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="form-group">
      <div className="select-with-icon">
        <Select 
          label="Category" 
          options={categories.map(cat => ({ value: cat.id, label: cat.title }))} 
          value={selectedCategoryId} 
          onChange={(e) => onSelect(e.target.value)} 
        />
        <button className="icon" onClick={handleAddCategoryClick}>
          <Icon name="add" />
        </button>
      </div>
    </div>
  );
}

export default SelectFeedCategory;
