import React from 'react';
import Modal from '../../../components/ui/modal.jsx';
import Input from '../../../components/forms/input.jsx';
import SelectFeedCategory from '../../../components/ui/select-feed-category.jsx';

function NewFeedModal({ isOpen, onClose, newFeed, setNewFeed, categories, handleAddFeed, handleAddCategory }) {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Add New Feed">
      <div className="form">
        <Input 
          label="Title" 
          name="title"
          value={newFeed.title} 
          onChange={(e) => setNewFeed({ ...newFeed, title: e.target.value })} 
        />
        <Input 
          label="URL" 
          name="url"
          value={newFeed.url} 
          onChange={(e) => setNewFeed({ ...newFeed, url: e.target.value })} 
        />
        <SelectFeedCategory 
          categories={categories} 
          selectedCategoryId={newFeed.categoryId} 
          onSelect={(categoryId) => setNewFeed({ ...newFeed, categoryId })} 
          onAddCategory={handleAddCategory} 
        />
        <div className="form-row">
          <Input 
            label="Check Interval (Hours)" 
            name="intervals_hrs"
            type="number" 
            value={newFeed.checkIntervalHours} 
            onChange={(e) => setNewFeed({ ...newFeed, checkIntervalHours: e.target.value })} 
          />
          <Input 
            label="Check Interval (Minutes)" 
            name="intervals_mins"
            type="number" 
            value={newFeed.checkIntervalMinutes} 
            onChange={(e) => setNewFeed({ ...newFeed, checkIntervalMinutes: e.target.value })} 
          />
        </div>
      </div>
      <div className="buttons">
        <button onClick={handleAddFeed}>Create Feed</button>
        <button className="cancel" onClick={onClose}>Cancel</button>
      </div>
    </Modal>
  );
}

export default NewFeedModal;
