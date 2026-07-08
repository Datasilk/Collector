import React, { useState, useEffect } from 'react';
import { useSession } from '../../context/session';
import { Feeds as FeedsApi } from '../../api/user/feeds';
import FeedsLayout from './layout';
import NewFeedModal from './components/new-feed-modal';
import Input from '../../components/forms/input.jsx';
import Select from '../../components/forms/select.jsx';
import { Accordion } from '../../components/ui/accordion';
import './page.css';

function FeedsPage() {
  const session = useSession();
  const { getCategories, addFeed, addCategory, getFilteredFeeds } = FeedsApi(session);
  const [feeds, setFeeds] = useState([]);
  const [categories, setCategories] = useState([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [newFeed, setNewFeed] = useState({ title: '', url: '', categoryId: '', checkIntervalHours: 0, checkIntervalMinutes: 0 });
  const [filter, setFilter] = useState({ search: '', sort: 'title', start: 0, length: 20 });
  const [debounceTimer, setDebounceTimer] = useState(null);

  useEffect(() => {
    loadCategories();
    loadFeeds();
  }, [filter]);

  const loadFeeds = async () => {
    const response = await getFilteredFeeds(filter);
    if (response.data && response.data.data && response.data.success) {
      setFeeds(response.data.data);
    }
  };

  const loadCategories = async () => {
    const response = await getCategories();
    if (response.data && response.data.data && response.data.success) {
      setCategories(response.data.data);
    }
  };

  const handleAddFeed = async () => {
    const checkInterval = (parseInt(newFeed.checkIntervalHours) * 60) + parseInt(newFeed.checkIntervalMinutes);
    const response = await addFeed({ ...newFeed, checkInterval });
    if (response.data && response.data.success) {
      await loadFeeds();
      setIsModalOpen(false);
      setNewFeed({ title: '', url: '', categoryId: '', checkIntervalHours: 0, checkIntervalMinutes: 0 });
    }
  };

  const handleAddCategory = async (categoryName) => {
    const response = await addCategory({ title: categoryName });
    if (response.data && response.data.success) {
      const categoriesResponse = await getCategories();
      if (categoriesResponse.data && categoriesResponse.data.data && categoriesResponse.data.success) {
        const updatedCategories = categoriesResponse.data.data;
        const newCategory = updatedCategories.find(c => c.title === categoryName);
        setCategories(updatedCategories);
        if (newCategory) {
          setNewFeed({ ...newFeed, categoryId: newCategory.categoryId });
        }
      }
    }
  };

  const handleSearchChange = (e) => {
    const searchValue = e.target.value;
    setFilter({ ...filter, search: searchValue });
    if (debounceTimer) {
      clearTimeout(debounceTimer);
    }
    setDebounceTimer(setTimeout(() => loadFeeds(), 500));
  };

  const handleSortChange = (e) => {
    setFilter({ ...filter, sort: e.target.value });
  };

  const groupedFeeds = categories.map(category => ({
    category,
    feeds: feeds.filter(feed => feed.categoryId === category.categoryId)
  })).filter(group => group.feeds.length > 0);

  return (
    <FeedsLayout>
      <div className="page">
        <div className="tool-bar">
          <div className="filters right-side btn-new-feed">
            <button onClick={() => setIsModalOpen(true)}>+ New Feed</button>
          </div>
          <div className="filters right-side">
            <Input
              placeholder="Search feeds..."
              value={filter.search}
              onChange={handleSearchChange}
            />
            <div className="sorting">
              <label>Sort By</label>
              <Select
                options={[
                  { value: "title", label: "Title" },
                  { value: "url", label: "URL" },
                  { value: "checkInterval", label: "Check Interval" }
                ]}
                value={filter.sort}
                onChange={handleSortChange}
              />
            </div>
          </div>
        </div>
        {groupedFeeds.length > 0 ? (
          groupedFeeds.map(group => (
            <Accordion
              key={group.category.categoryId}
              title={group.category.title}
              className="feed-category-row"
            >
              <div className="feed-cards">
                {group.feeds.map(feed => (
                  <div key={feed.feedId} className="entry-card feed-card">
                    <h5>{feed.title}</h5>
                    <p className="url">{feed.url}</p>
                    <p className="interval">
                      every {isNaN(feed.checkInterval) ? '24 hours' : (feed.checkInterval >= 60 ? `${Math.floor(feed.checkInterval / 60)} hours` : '')}
                      {isNaN(feed.checkInterval) ? '' : (feed.checkInterval % 60 !== 0 ? `${feed.checkInterval % 60} minutes` : '')}
                    </p>
                  </div>
                ))}
              </div>
            </Accordion>
          ))
        ) : (
          <p>No feeds or categories to display.</p>
        )}
        {isModalOpen &&
          <NewFeedModal 
            isOpen={isModalOpen} 
            onClose={() => setIsModalOpen(false)} 
            newFeed={newFeed} 
            setNewFeed={setNewFeed} 
            categories={categories} 
            handleAddFeed={handleAddFeed} 
            handleAddCategory={handleAddCategory} 
          />
        }
      </div>
    </FeedsLayout>
  );
}

export default FeedsPage;
