import React, { useState, useEffect } from 'react';
//styles
import '@/styles/admin/filter.css';
import './page.css';
//components
import Container from '@/components/admin/container';
import Modal from '@/components/ui/modal';
import Icon from '@/components/ui/icon';
import Select from '@/components/forms/select';
import Input from '@/components/forms/input';
import ArticleModal from './components/article-modal';
import Pager from '@/components/ui/pager';
//context
import { useSession } from '@/context/session';
//api
import { Articles } from '@/api/user/articles';
import { Downloads } from '@/api/user/downloads';
//helpers
import { handleSort, getSortIcon } from '@/helpers/format';
import { localDateTime, printDate } from '@/helpers/datetime';
import messages from '@/helpers/messages';

/**
 * <summary>Admin Articles List Page</summary>
 * <description>Displays and manages the list of articles in the admin panel.</description>
 */
export default function AdminArticles() {
    const session = useSession();
    const { getArticles, removeArticle } = Articles(session);
    const { addQueueItem } = Downloads(session);

    const [articles, setArticles] = useState([]);
    const [totalCount, setTotalCount] = useState(0);
    const [showAdd, setShowAdd] = useState(false);
    const [searchTitle, setSearchTitle] = useState('');
    const [categoryFilters, setCategoryFilters] = useState(0);
    const [categoryFiltersList, setCategoryFiltersList] = useState([
        { id: 0, name: 'All Categories' },
        { id: 1, name: 'Technology' },
        { id: 2, name: 'Science' },
        { id: 3, name: 'Finance' },
        { id: 4, name: 'Sports' },
        { id: 5, name: 'News' }
    ]);
    const [sort, setSort] = useState('Published DESC');
    const [deleteModal, setDeleteModal] = useState(null);
    const [selectedArticle, setSelectedArticle] = useState(null);
    const [currentPage, setCurrentPage] = useState(1);
    const pageSize = 100;

    useEffect(() => {
        // Fetch articles from API when component mounts
        fetchArticles();
    }, []);

    useEffect(() => {
        filterArticles();
    }, [searchTitle, categoryFilters, sort, currentPage]);

    const fetchArticles = () => {
        // Call the API with the proper ArticleListRequestModel
        getArticles({
            search: searchTitle || '',
            subjectIds: categoryFilters !== 0 ? [categoryFilters] : [],
            orderBy: getOrderByValue(sort),
            start: (currentPage - 1) * pageSize + 1,
            length: pageSize,
            isActive: 0 // ArticleIsActive.Both
        }).then(response => {
            if (response.data.success) {
                setArticles(response.data.data.articles || []);
                setTotalCount(response.data.data.totalCount || 0);
            }
        }).catch(error => {
            console.error('Error fetching articles:', error);
        });
    };
    
    const getOrderByValue = (sortString) => {
        // Convert sort string to ArticleSortBy enum value
        const [field, direction] = sortString.split(' ');
        switch (field.toLowerCase()) {
            case 'title': return direction === 'ASC' ? 5 : 6; // Title ASC/DESC
            case 'domain': return direction === 'ASC' ? 7 : 8; // Domain ASC/DESC
            case 'published': return direction === 'ASC' ? 3 : 4; // Date ASC/DESC
            case 'views': return direction === 'ASC' ? 9 : 10; // Views ASC/DESC
            default: return 0; // BestScore
        }
    };
    
    const filterArticles = () => {
        fetchArticles();
    };

    const handleDelete = (article) => {
        setDeleteModal(article);
    };

    const handleViewArticle = (article) => {
        setSelectedArticle(article);
    };

    const handleCloseArticleModal = () => {
        setSelectedArticle(null);
    };

    const handleDownloadArticle = (article) => {
        if (!article.url || !article.domain) {
            messages.error('Article is missing URL or domain');
            return;
        }
        addQueueItem(article.url, article.domain, 0, article.feedId || 0)
            .then(response => {
                if (response.data.success) {
                    messages.success('Article queued for download');
                } else {
                    messages.error(response.data.message || 'Failed to queue article');
                }
            })
            .catch(error => {
                console.error('Error queueing article:', error);
                messages.error('Failed to queue article for download');
            });
    };

    const handlePageChange = (page) => {
        setCurrentPage(page);
    };

    const totalPages = Math.ceil(totalCount / pageSize) || 1;

    const handleDeleteClose = () => {
        setDeleteModal(null);
    };

    const handleDeleteConfirmed = (articleId) => {
        // Call the API to delete the article
        removeArticle(articleId).then(response => {
            if (response.data.success) {
                setArticles(articles.filter(article => article.articleId !== articleId));
                messages.success('Article deleted successfully');
            } else {
                messages.error('Failed to delete article');
            }
            handleDeleteClose();
        }).catch(error => {
            console.error('Error deleting article:', error);
            messages.error('An error occurred while deleting the article');
            handleDeleteClose();
        });
    };

    const DeleteModal = () => {
        return (<>
            <Modal
                title="Delete Article"
                onClose={handleDeleteClose}
            >
                <p>
                    Do you really want to delete the article "{deleteModal.title}"?
                    <br />
                    This will permanently remove the article from the system.
                </p>
                <div className="buttons">
                    <button className="submit" onClick={() => { handleDeleteConfirmed(deleteModal.articleId) }}>Yes</button>
                    <button className="cancel" onClick={handleDeleteClose}>Cancel</button>
                </div>
            </Modal>
        </>);
    };

    const handleClosedAddArticle = (article) => {
        if(article) {
            // Refresh the article list to include the newly added article
            fetchArticles();
        }
        setShowAdd(false);
    };

    const tools = (<>
        <button onClick={() => setShowAdd(true)}><Icon name="add"></Icon>New Article</button>
    </>);

    return (
        <div className="admin-articles">
            {showAdd && <div className="modal-placeholder">Add Article Modal would appear here</div>}
            {deleteModal != null && <DeleteModal></DeleteModal>}
            {selectedArticle != null && (
                <ArticleModal
                    article={selectedArticle}
                    onClose={handleCloseArticleModal}
                    onDownload={handleDownloadArticle}
                />
            )}
            <Container
                title="Article Management"
                tools={tools}
            >
                <div className="filters">
                    <Input
                        name="articlesearch"
                        type="text"
                        placeholder="Search by Title"
                        value={searchTitle}
                        onInput={(e) => setSearchTitle(e.target.value)}
                        className="titleInput"
                    />
                    <Select
                        options={categoryFiltersList.map(category => ({ value: category.id, label: category.name }))}
                        value={categoryFilters}
                        onChange={(e) => setCategoryFilters(e.target.value)}
                    />
                </div>
                <table className="spreadsheet">
                    <thead>
                        <tr>
                            <th onClick={() => setSort(handleSort('Title', sort))}>
                                Title {getSortIcon('Title', sort) && <span className="material-symbols-rounded">{getSortIcon('Title', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('Domain', sort))}>
                                Domain {getSortIcon('Domain', sort) && <span className="material-symbols-rounded">{getSortIcon('Domain', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('Published', sort))}>
                                Published {getSortIcon('Published', sort) && <span className="material-symbols-rounded">{getSortIcon('Published', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('Status', sort))}>
                                Status {getSortIcon('Status', sort) && <span className="material-symbols-rounded">{getSortIcon('Status', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('Views', sort))}>
                                Views {getSortIcon('Views', sort) && <span className="material-symbols-rounded">{getSortIcon('Views', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('Subject', sort))}>
                                Subject {getSortIcon('Subject', sort) && <span className="material-symbols-rounded">{getSortIcon('Subject', sort)}</span>}
                            </th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        {articles.map(article =>
                            <tr
                                key={article.articleId}
                                onClick={(e) => {
                                    // Prevent triggering if the event originated from action buttons
                                    if (e.target.closest('a')) {
                                        e.stopPropagation();
                                        return;
                                    }
                                    handleViewArticle(article);
                                }}
                            >
                                <td>{article.title}</td>
                                <td>{article.domain}</td>
                                <td>{article.datepublished ? printDate(localDateTime(new Date(article.datepublished))) : 'N/A'}</td>
                                <td>{article.active === true ? 'Active' : article.active === false ? 'Inactive' : 'N/A'}</td>
                                <td>{article.visited}</td>
                                <td>{article.subjectTitle}</td>
                                <td className="buttons">
                                    <button className="icon" onClick={() => handleViewArticle(article)} title="view article">
                                        <Icon name="visibility"></Icon>
                                    </button>
                                    <button className="icon" onClick={() => handleDownloadArticle(article)} title="download article">
                                        <Icon name="download"></Icon>
                                    </button>
                                    <button className="icon" onClick={() => handleDelete(article)} title="delete article">
                                        <Icon name="delete"></Icon>
                                    </button>
                                </td>
                            </tr>
                        )}
                    </tbody>
                </table>
                <Pager
                    currentPage={currentPage}
                    totalPages={totalPages}
                    onPageChange={handlePageChange}
                />
            </Container>
        </div>
    );
}
