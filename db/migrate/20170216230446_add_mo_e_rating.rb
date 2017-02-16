class AddMoERating < ActiveRecord::Migration[5.0]
  def change
  
	add_column :players, :moe_rating, :integer
	add_column :tanks, :moe_value, :integer
	add_column :clans, :moe_rating, :integer
  end
end
