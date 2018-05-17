﻿using autotrade.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static autotrade.Interfaces.Steam.TradeOffer.Inventory;

namespace autotrade.CustomElements {
    class AllItemsListGridUtils {

        public static DataGridViewRow GetRowByItemMarketHashName(DataGridView allItemsGrid, string marketHashName) {
            for (int i = 0; i < allItemsGrid.RowCount; i++) {
                var hidenMarketHashNameCell = GetGridHidenItemMarketHashNameCell(allItemsGrid, i);

                if ((string)hidenMarketHashNameCell.Value == marketHashName) {
                    return allItemsGrid.Rows[i];
                }
            }

            return null;
        }

        public static void FillSteamSaleDataGrid(DataGridView allItemsGrid, List<RgFullItem> itemsToAddList) {

            var groupedItems = itemsToAddList.GroupBy(item => item.Description.market_hash_name);

            foreach (var itemsGroup in groupedItems) {
                AddNewItemCellAllItemsDataGridView(allItemsGrid, itemsGroup.ToList());

                var firstItem = itemsGroup.First();
                if (!SaleControl.AllDescriptionsDictionary.ContainsKey(firstItem.Description.market_hash_name)) {
                    SaleControl.AllDescriptionsDictionary.Add(firstItem.Description.market_hash_name, firstItem.Description);
                }
            }
        }

        public static void AddNewItemCellAllItemsDataGridView(DataGridView allItemsGrid, List<RgFullItem> itemsToAddList) {
            int currentRowNumber = allItemsGrid.Rows.Add();

            var nameCell = GetGridNameTextBoxCell(allItemsGrid, currentRowNumber);
            var countCell = GetGridCountTextBoxCell(allItemsGrid, currentRowNumber);
            var comboBoxCell = GetGridCountToAddComboBoxCell(allItemsGrid, currentRowNumber);
            var hidenItemsListCell = GetGridHidenItemsListCell(allItemsGrid, currentRowNumber);
            var hidenMarketHashNameCell = GetGridHidenItemMarketHashNameCell(allItemsGrid, currentRowNumber);

            var firstItem = itemsToAddList.First();

            nameCell.Value = firstItem.Description.name;
            countCell.Value = itemsToAddList.Sum(item => int.Parse(item.Asset.amount));
            SetDefaultAmountToAddComboBoxCellValue(comboBoxCell);
            hidenItemsListCell.Value = itemsToAddList;
            hidenMarketHashNameCell.Value = firstItem.Description.market_hash_name;
        }

        public static void UpdateItemDescription(DataGridView allItemsGrid, int row, RichTextBox textBox, Panel imageBox, Label label) {
            var cell = GetGridHidenItemsListCell(allItemsGrid, row);
            if (cell == null) return;

            var hidenItemsList = (List<RgFullItem>)cell.Value;
            if (hidenItemsList == null) return;

            UpdateItemDescription(SaleControl.AllDescriptionsDictionary[hidenItemsList.First().Description.market_hash_name], textBox, imageBox, label);
        }

        public static void UpdateItemDescription(RgDescription description, RichTextBox textBox, Panel imageBox, Label label) {
            SaleControl.LastSelectedItemDescription = description;

            UpdateItemTextDescription(description, textBox, label);
            UpdateItemImage(description, imageBox);
        }

        public static void GridComboBoxClick(DataGridView allItemsGrid, int row) {
            var comboBoxCell = GetGridCountToAddComboBoxCell(allItemsGrid, row);
            int leftItemsCount = int.Parse(GetGridCountTextBoxCell(allItemsGrid, row).Value.ToString());

            if (comboBoxCell.Items.Count != leftItemsCount + 1) {
                FillComboBoxCellFromZeroToMaxValue(comboBoxCell, leftItemsCount);
            }
            allItemsGrid.BeginEdit(true);
            comboBoxCell.Selected = true;
            ((DataGridViewComboBoxEditingControl)allItemsGrid.EditingControl).DroppedDown = true;
        }

        public static void GridAddButtonClick(DataGridView allItemsGrid, int row, DataGridView itemsToSaleGrid) {
            var nameTextBoxCell = GetGridNameTextBoxCell(allItemsGrid, row);
            var countTextBoxCell = GetGridCountTextBoxCell(allItemsGrid, row);
            var amountToAddComboBoxCell = GetGridCountToAddComboBoxCell(allItemsGrid, row);
            var hidenItemsList = (List<RgFullItem>)GetGridHidenItemsListCell(allItemsGrid, row).Value;

            int itemsToAddCount = int.Parse(amountToAddComboBoxCell.Value.ToString());
            if (itemsToAddCount <= 0) return;

            var itemsToSell = new List<RgFullItem>();

            int addedCount = 0;
            for (int i = hidenItemsList.Count - 1; i >= 0; i--) {
                var item = hidenItemsList[i];

                if (item.Asset.amount.Equals("1")) { //если это не стакающийся предмет
                    itemsToSell.Add(item);
                    hidenItemsList.RemoveAt(i);
                    addedCount++;
                }
                else { //если это стакающийся предмет
                    int itemsInSlotCount = int.Parse(item.Asset.amount);
                    if (itemsInSlotCount + addedCount > itemsToAddCount) { //если нам нужно добавить НЕ ВСЕ предметы с 1 слота
                        int neededCount = itemsToAddCount - addedCount;
                        var neededAmountOfItem = item.CloneAsset();
                        neededAmountOfItem.Asset.amount = neededCount.ToString();
                        itemsToSell.Add(neededAmountOfItem);
                        item.Asset.amount = (int.Parse(item.Asset.amount) - neededCount).ToString();
                    }
                    else { //если нам нужно добавить ВСЕ предметы с 1 слота
                        itemsToSell.Add(item);
                        hidenItemsList.RemoveAt(i);
                        addedCount += itemsInSlotCount;
                    }
                }
                if (addedCount == itemsToAddCount) break;
            }

            ItemsToSaleGridUtils.AddItemsToSale(itemsToSaleGrid, itemsToSell);

            int totalAmount = int.Parse(countTextBoxCell.Value.ToString()); //изменение ячейки Общее количество
            if (itemsToAddCount == totalAmount) {
                allItemsGrid.Rows.RemoveAt(row);
            }
            else {
                countTextBoxCell.Value = totalAmount - itemsToAddCount;
                amountToAddComboBoxCell.Items.Add(0);
                amountToAddComboBoxCell.Value = 0;
            }
        }

        public static void GridAddAllButtonClick(DataGridView allItemsGrid, int row, DataGridView itemsToSaleGrid) {
            var nameTextBoxCell = GetGridNameTextBoxCell(allItemsGrid, row);
            var countTextBoxCell = GetGridCountTextBoxCell(allItemsGrid, row);
            var hidenItemsListCell = GetGridHidenItemsListCell(allItemsGrid, row);
            var hidenItemsList = (List<RgFullItem>)hidenItemsListCell.Value;

            int itemsCount = int.Parse(countTextBoxCell.Value.ToString());
            if (itemsCount <= 0) return;

            var amountToAddComboBoxCell = GetGridCountToAddComboBoxCell(allItemsGrid, row);

            ItemsToSaleGridUtils.AddItemsToSale(itemsToSaleGrid, hidenItemsList);

            allItemsGrid.Rows.RemoveAt(row);
        }

        public static void AddItemsToExistRow(DataGridView allItemsGrid, int row, List<RgFullItem> items) {
            var countTextBoxCell = GetGridCountTextBoxCell(allItemsGrid, row);
            var hidenItemsListCell = GetGridHidenItemsListCell(allItemsGrid, row);
            var hidenItemsList = (List<RgFullItem>)hidenItemsListCell.Value;

            if (hidenItemsList == null) {
                hidenItemsListCell.Value = items;
                hidenItemsList = (List<RgFullItem>)hidenItemsListCell.Value;
            }
            else {
                hidenItemsList.AddRange(items);
            }

            countTextBoxCell.Value = hidenItemsList.Sum(item => int.Parse(item.Asset.amount));
        }

        private static void FillComboBoxCellFromZeroToMaxValue(DataGridViewComboBoxCell comboBoxCell, int maxValue) {
            if (maxValue < 0) return;

            SetDefaultAmountToAddComboBoxCellValue(comboBoxCell);

            int step = 1;
            if (maxValue > 1000) {
                step = (maxValue.ToString().Length - 2) * 10;
            }

            for (int i = step; i <= maxValue; i += step) {
                comboBoxCell.Items.Add(i.ToString());
            }
        }

        private static void SetDefaultAmountToAddComboBoxCellValue(DataGridViewComboBoxCell comboBoxCell) {
            comboBoxCell.Value = null;
            comboBoxCell.Items.Clear();
            comboBoxCell.Items.Add("0");
            comboBoxCell.Value = "0";
        }

        private static void UpdateItemTextDescription(RgDescription description, RichTextBox textBox, Label label) {
            textBox.Clear();

            label.Text = description.name;

            var descriptionText = "";
            foreach (var item in description.descriptions) {
                string text = item.value.Trim();
                if (!string.IsNullOrWhiteSpace(text)) descriptionText += text + ", ";
            }
            if (descriptionText.EndsWith(", ")) descriptionText = descriptionText.Substring(0, descriptionText.Length - 2);

            var tagsText = "";
            foreach (var item in description.tags) {
                string text = item.localized_tag_name.Trim();
                if (!string.IsNullOrWhiteSpace(text)) tagsText += text + ", ";
            }
            if (tagsText.EndsWith(", ")) tagsText = tagsText.Substring(0, tagsText.Length - 2);

            appendBoldText(textBox, "Игра: ");
            textBox.AppendText(description.appid.ToString() + "\n");
            
            appendBoldText(textBox, "Название: ");
            textBox.AppendText(description.market_hash_name + "\n");

            appendBoldText(textBox, "Тип: ");
            textBox.AppendText(description.type + "\n");

            appendBoldText(textBox, "Передаваемый: ");
            textBox.AppendText(((description.tradable) ? "Да" : "Нет") + "\n");

            appendBoldText(textBox, "Продаваемый: ");
            textBox.AppendText(((description.marketable) ? "Да" : "Нет"));

            if (!string.IsNullOrWhiteSpace(descriptionText)) {
                textBox.AppendText("\n");
                appendBoldText(textBox, "Описание: ");
                textBox.AppendText(descriptionText);
            }

            if (!string.IsNullOrWhiteSpace(tagsText)) {
                textBox.AppendText("\n");
                appendBoldText(textBox, "Теги: ");
                textBox.AppendText(tagsText);
            }
        }

        private static void appendBoldText(RichTextBox textBox, string text) {
            textBox.SelectionFont = new Font(textBox.Font, FontStyle.Bold);
            textBox.AppendText(text);
            textBox.SelectionFont = new Font(textBox.Font, FontStyle.Regular);
        }

        private static void UpdateItemImage(RgDescription description, Panel imageBox) {
            Task.Run(() => {
                SaleControl.ImageDictionary.TryGetValue(description.market_hash_name, out Image image);

                if (image != null) {
                    imageBox.BackgroundImage = ImageUtils.ResizeImage(image, 100, 100);
                }
                else {
                    image = ImageUtils.DownloadImage("https://steamcommunity-a.akamaihd.net/economy/image/" + description.icon_url + "/192fx192f");
                    if (image != null) {
                        if (!SaleControl.ImageDictionary.ContainsKey(description.market_hash_name)) {
                            SaleControl.ImageDictionary.Add(description.market_hash_name, image);
                        }
                        imageBox.BackgroundImage = ImageUtils.ResizeImage(image, 100, 100);
                    }
                }
            });
        }

        #region Cells getters
        private static DataGridViewTextBoxCell GetGridNameTextBoxCell(DataGridView grid, int rowIndex) {
            return (DataGridViewTextBoxCell)grid.Rows[rowIndex].Cells[0];
        }

        private static DataGridViewTextBoxCell GetGridCountTextBoxCell(DataGridView grid, int rowIndex) {
            return (DataGridViewTextBoxCell)grid.Rows[rowIndex].Cells[1];
        }

        private static DataGridViewComboBoxCell GetGridCountToAddComboBoxCell(DataGridView grid, int rowIndex) {
            return (DataGridViewComboBoxCell)grid.Rows[rowIndex].Cells[2];
        }

        private static DataGridViewButtonCell GetGridAddButtonCell(DataGridView grid, int rowIndex) {
            return (DataGridViewButtonCell)grid.Rows[rowIndex].Cells[3];
        }

        private static DataGridViewButtonCell GetGridAddAllButtonCell(DataGridView grid, int rowIndex) {
            return (DataGridViewButtonCell)grid.Rows[rowIndex].Cells[4];
        }

        private static DataGridViewTextBoxCell GetGridHidenItemsListCell(DataGridView grid, int rowIndex) {
            return (DataGridViewTextBoxCell)grid.Rows[rowIndex].Cells[5];
        }

        private static DataGridViewTextBoxCell GetGridHidenItemImageCell(DataGridView grid, int rowIndex) {
            return (DataGridViewTextBoxCell)grid.Rows[rowIndex].Cells[6];
        }

        private static DataGridViewTextBoxCell GetGridHidenItemMarketHashNameCell(DataGridView grid, int rowIndex) {
            return (DataGridViewTextBoxCell)grid.Rows[rowIndex].Cells[7];
        }
        #endregion
    }
}
